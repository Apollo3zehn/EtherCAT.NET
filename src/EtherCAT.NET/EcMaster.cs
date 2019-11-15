using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using EtherCAT.NET.Extensibility;
using EtherCAT.NET.Extension;
using EtherCAT.NET.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OneDas.Extensibility;
using OneDas.Infrastructure;
using SOEM.PInvoke;

namespace EtherCAT.NET
{
    public class EcMaster : IDisposable
    {
        #region Fields

        // general
        IExtensionFactory _extensionFactory;
        private ILogger _logger;
        private EcSettings _settings;

        // data
        private IntPtr _ioMapPtr;
        private int _actualIoMapSize;

        // conversion
        private const long _dateTime_To_Ns = 100L;

        // DC
        private const int _dcRingBufferSize = 100;
        private int _dcDriftCompensationRate;
        private int _dcRingBufferIndex;
        private long[] _dcRingBuffer;
        private bool _isDcCompensationRunning;
        private DateTime _dcEpoch;

        // timing
        private long _dcTime;
        private TimeSpan _offset;

        // diagnostics
        private int _counter;
        private int _expectedWorkingCounter;
        private int _actualWorkingCounter;
        private int _lostFrameCounter;
        private int _wkcMismatchCounter;
        private int _statusCheckFailedCounter;
        private object _lock;
        private bool _isReconfiguring;
        private CancellationTokenSource _cts;
        private Task _watchdogTask;

        #endregion

        #region Constructors

        public EcMaster(EcSettings settings, IExtensionFactory extensionFactory) : this(settings, extensionFactory, NullLogger.Instance)
        {
            //
        }

        public EcMaster(EcSettings settings, IExtensionFactory extensionFactory, ILogger logger)
        {
            _settings = settings.ShallowCopy();
            _logger = logger;
            _extensionFactory = extensionFactory;

            this.Context = EcHL.CreateContext();

            // DC
            _dcRingBuffer = new long[_dcRingBufferSize];
            _dcEpoch = new DateTime(2000, 1, 1);
            _dcDriftCompensationRate = Convert.ToInt32(_settings.DriftCompensationRate / _settings.CycleFrequency); // 850 ppm max clock drift

            // data
            _ioMapPtr = Marshal.AllocHGlobal(_settings.IoMapLength);

            unsafe
            {
                new Span<byte>(_ioMapPtr.ToPointer(), _settings.IoMapLength).Clear();
            }

            // diagnostics
            _isReconfiguring = false;
            _lock = new object();
            _cts = new CancellationTokenSource();
        }

        #endregion

        #region Properties

        public IntPtr Context { get; private set; }
        public DateTime UtcDateTime { get; private set; }
        public long DcRingBufferAverage { get; private set; }

        #endregion

        #region Methods

        private void ValidateSlaves(IList<SlaveInfo> slaveInfoSet, IList<SlaveInfo> actualSlaveInfoSet)
        {
            if (slaveInfoSet.Count() != actualSlaveInfoSet.Count())
            {
                throw new Exception(ErrorMessage.EthercatGateway_EtherCATConfigurationMismatch);
            }

            for (int i = 0; i <= actualSlaveInfoSet.Count() - 1; i++)
            {
                if (!(actualSlaveInfoSet[i].ProductCode == slaveInfoSet[i].ProductCode && actualSlaveInfoSet[i].Revision == slaveInfoSet[i].Revision))
                {
                    throw new Exception(ErrorMessage.EthercatGateway_EtherCATConfigurationMismatch);
                }
            }
        }

        private void ConfigureSlaves(IList<SlaveInfo> slaveInfoSet)
        {
            List<EcHL.PO2SOCallback> callbackSet;

            callbackSet = new List<EcHL.PO2SOCallback>();

            foreach (SlaveInfo slaveInfo in slaveInfoSet)
            {
                ushort currentSlaveIndex;
                IEnumerable<SlaveExtensionLogic> extensionSet;
                IEnumerable<SdoWriteRequest> sdoWriteRequestSet;
                EcHL.PO2SOCallback callback;

                // SDO / PDO config / PDO assign
                currentSlaveIndex = (ushort)(Convert.ToUInt16(slaveInfoSet.ToList().IndexOf(slaveInfo)) + 1);
                extensionSet = slaveInfo.SlaveExtensionSet.Select(slaveExtension => _extensionFactory.BuildLogic<SlaveExtensionLogic>(slaveExtension)).ToList();

                sdoWriteRequestSet = slaveInfo.GetConfiguration(extensionSet).ToList();

                callback = slaveIndex =>
                {
                    sdoWriteRequestSet.ToList().ForEach(sdoWriteRequest =>
                    {
                        EcUtilities.CheckErrorCode(this.Context, EcUtilities.SdoWrite(this.Context, slaveIndex, sdoWriteRequest.Index, sdoWriteRequest.SubIndex, sdoWriteRequest.Dataset), nameof(EcHL.SdoWrite));
                    });

                    return 0;
                };

                EcHL.RegisterCallback(this.Context, currentSlaveIndex, callback);
                callbackSet.Add(callback);
            }

            callbackSet.ForEach(callback =>
            {
                GC.KeepAlive(callback);
            });
        }

        private void ConfigureIoMap(IList<SlaveInfo> slaveInfoSet)
        {
            int ioMapByteOffset;
            int ioMapBitOffset;
            int[] slavePdoOffsetSet;
            int[] slaveRxPdoOffsetSet;
            int[] slaveTxPdoOffsetSet;

            ioMapByteOffset = 0;
            ioMapBitOffset = 0;
            slavePdoOffsetSet = null;
            slaveRxPdoOffsetSet = new int[slaveInfoSet.Count() + 1];
            slaveTxPdoOffsetSet = new int[slaveInfoSet.Count() + 1];

            _actualIoMapSize = EcHL.ConfigureIoMap(this.Context, _ioMapPtr, slaveRxPdoOffsetSet, slaveTxPdoOffsetSet, out _expectedWorkingCounter);

            foreach (DataDirection dataDirection in Enum.GetValues(typeof(DataDirection)))
            {
                switch (dataDirection)
                {
                    case DataDirection.Output:
                        slavePdoOffsetSet = slaveRxPdoOffsetSet;
                        break;
                    case DataDirection.Input:
                        slavePdoOffsetSet = slaveTxPdoOffsetSet;
                        break;
                    default:
                        throw new NotImplementedException();
                }

                foreach (SlaveInfo SlaveInfo in slaveInfoSet)
                {
                    ioMapByteOffset = slavePdoOffsetSet[slaveInfoSet.ToList().IndexOf(SlaveInfo) + 1];

                    foreach (SlaveVariable variable in SlaveInfo.DynamicData.PdoSet.Where(x => x.SyncManager >= 0).ToList().SelectMany(x => x.VariableSet).ToList().Where(x => x.DataDirection == dataDirection))
                    {
                        variable.DataPtr = IntPtr.Add(_ioMapPtr, ioMapByteOffset);
                        variable.BitOffset = ioMapBitOffset;

                        if (variable.DataType == OneDasDataType.BOOLEAN)
                        {
                            variable.BitOffset = ioMapBitOffset; // bool is treated as bit-oriented
                        }

                        Debug.WriteLine($"{ variable.Name } { variable.DataPtr.ToInt64() - _ioMapPtr.ToInt64() }/{ variable.BitOffset }");

                        ioMapBitOffset += variable.BitLength;

                        if (ioMapBitOffset > 7)
                        {
                            ioMapBitOffset = ioMapBitOffset % 8;
                            ioMapByteOffset += (variable.BitLength + 7) / 8;
                        }
                    }
                }
            }

            _logger.LogInformation($"IO map configured ({slaveInfoSet.Count()} {(slaveInfoSet.Count() > 1 ? "slaves" : "slave")}, {_actualIoMapSize} bytes)");
        }

        private void ConfigureDc()
        {
            uint systemTimeDifference;
            EcUtilities.CheckErrorCode(this.Context, EcHL.ConfigureDc(this.Context, _settings.FrameCount, _settings.TargetTimeDifference, out systemTimeDifference), nameof(EcHL.ConfigureDc));

            _logger.LogInformation($"DC system time diff. is <= { systemTimeDifference & 0x7FFF } ns");
        }

        private void ConfigureSync01(IList<SlaveInfo> slaveInfoSet)
        {
            // SYNC0 / SYNC1
            foreach (SlaveInfo slaveInfo in slaveInfoSet)
            {
                ushort slaveIndex;
                DistributedClocksSettings distributedClocksSettings;

                slaveIndex = (ushort)(Convert.ToUInt16(slaveInfoSet.ToList().IndexOf(slaveInfo)) + 1);
                distributedClocksSettings = slaveInfo.SlaveExtensionSet.OfType<DistributedClocksSettings>().ToList().FirstOrDefault();

                if (distributedClocksSettings != null)
                {
                    DistributedClocksParameters parameters;
                    byte[] assignActivate;

                    if (!slaveInfo.SlaveEsi.Dc.TimeLoopControlOnly)
                    {
                        assignActivate = null;
                        parameters = distributedClocksSettings.CalculateDcParameters(ref assignActivate, _settings.CycleFrequency);
                        EcUtilities.CheckErrorCode(this.Context, EcHL.ConfigureSync01(this.Context, slaveIndex, ref assignActivate, assignActivate.Count(), parameters.CycleTime0, parameters.CycleTime1, parameters.ShiftTime0));
                    }
                }
            }
        }

        public void Configure(SlaveInfo rootSlaveInfo = null)
        {
            SlaveInfo actualSlaveInfo;
            IList<SlaveInfo> slaveInfoSet;
            IList<SlaveInfo> actualSlaveInfoSet;
            NetworkInterface networkInterface;

            networkInterface = NetworkInterface.GetAllNetworkInterfaces().Where(nic => nic.Name == _settings.InterfaceName).FirstOrDefault();

            if (networkInterface?.OperationalStatus != OperationalStatus.Up)
            {
                throw new Exception($"Network interface '{_settings.InterfaceName}' is not linked. Aborting action.");
            }

            #region "PreOp"

            actualSlaveInfo = EcUtilities.ScanDevices(this.Context, _settings.InterfaceName, null);

            if (rootSlaveInfo == null)
            {
                rootSlaveInfo = actualSlaveInfo;

                rootSlaveInfo.Descendants().ToList().ForEach(current =>
                {
                    ExtensibilityHelper.CreateDynamicData(_settings.EsiDirectoryPath, _extensionFactory, current);
                });
            }

            slaveInfoSet = rootSlaveInfo.Descendants().ToList();
            actualSlaveInfoSet = actualSlaveInfo.Descendants().ToList();

            this.ValidateSlaves(slaveInfoSet, actualSlaveInfoSet);
            this.ConfigureSlaves(slaveInfoSet);
            this.ConfigureIoMap(slaveInfoSet);
            this.ConfigureDc();
            this.ConfigureSync01(slaveInfoSet);

            #endregion

            #region "SafeOp"

            EcUtilities.CheckErrorCode(this.Context, EcHL.CheckSafeOpState(this.Context), nameof(EcHL.CheckSafeOpState));

            #endregion

            #region "Op"

            EcUtilities.CheckErrorCode(this.Context, EcHL.RequestOpState(this.Context), nameof(EcHL.RequestOpState));

            #endregion

            if (_watchdogTask == null)
            {
                _watchdogTask = Task.Run(() => this.WatchdogRoutine(), _cts.Token);
            }
        }

        public void UpdateIO(DateTime referenceDateTime)
        {
            lock (_lock)
            {
                if (_isReconfiguring)
                {
                    return;
                }

                _counter = (int)((_counter + 1) % _settings.CycleFrequency);
                _actualWorkingCounter = EcHL.UpdateIo(this.Context, out _dcTime);

                #region "Diagnostics"

                // statistics
                if (_counter == 0)
                {
                    Trace.WriteLine($"lost frames: {(double)_lostFrameCounter / _settings.CycleFrequency:P2} / wkc mismatch: {(double)_wkcMismatchCounter / _settings.CycleFrequency:P2}");

                    if (_lostFrameCounter == _settings.CycleFrequency)
                    {
                        _logger.LogWarning($"frame loss occured ({ _settings.CycleFrequency } frames)");
                        _lostFrameCounter = 0;
                    }

                    if (_wkcMismatchCounter == _settings.CycleFrequency)
                    {
                        _logger.LogWarning($"working counter mismatch { _actualWorkingCounter }/{ _expectedWorkingCounter }");
                        //Trace.WriteLine(EcUtilities.GetSlaveStateDescription(_ecSettings.RootSlaveInfoSet.SelectMany(x => x.Descendants()).ToList()));
                        _wkcMismatchCounter = 0;
                    }

                    _lostFrameCounter = 0;
                    _wkcMismatchCounter = 0;
                }

                // no frame
                if (_actualWorkingCounter == -1)
                {
                    _lostFrameCounter += 1;

                    this.UtcDateTime = DateTime.MinValue;
                    this.DcRingBufferAverage = 0;

                    return;
                }

                // working counter mismatch
                if (_expectedWorkingCounter != _actualWorkingCounter)
                {
                    _wkcMismatchCounter += 1;

                    this.UtcDateTime = DateTime.MinValue;
                    this.DcRingBufferAverage = 0;

                    return;
                }

                #endregion

                // the UpdateIo timer tries to fire at discrete times. The timer is allowed to be early or delayed by < (CycleTime - Offset) and the resulting DC time will be floored to nearest 10 ms. 
                this.UtcDateTime = _dcEpoch.AddTicks(Convert.ToInt64(_dcTime / _dateTime_To_Ns));  // for time loop control

                // for compensation, if DC is not initialized with real time or not initialized at all
                if (_offset == TimeSpan.Zero)
                    _offset = referenceDateTime - this.UtcDateTime;
                else
                    this.UtcDateTime += _offset;

                // dc drift compensation
                _dcRingBuffer[_dcRingBufferIndex] = Convert.ToInt64(referenceDateTime.Ticks - this.UtcDateTime.Ticks) * _dateTime_To_Ns;
                this.DcRingBufferAverage = Convert.ToInt64(_dcRingBuffer.Average());

                if (!_isDcCompensationRunning && Math.Abs(this.DcRingBufferAverage) > 1500000) // 1.5 ms
                {
                    _isDcCompensationRunning = true;
                    _logger.LogInformation("DC drift compensation started");
                }
                else if (_isDcCompensationRunning && Math.Abs(this.DcRingBufferAverage) < 1000000) // 1.0 ms
                {
                    _isDcCompensationRunning = false;
                    _logger.LogInformation("DC drift compensation finished");
                }

                if (_isDcCompensationRunning)
                {
                    EcHL.CompensateDcDrift(this.Context, Convert.ToInt32(Math.Min(Math.Max(this.DcRingBufferAverage, -_dcDriftCompensationRate), _dcDriftCompensationRate)));
                }

                _dcRingBufferIndex = (_dcRingBufferIndex + 1) % _dcRingBufferSize;
            }
        }

        #endregion

        private void WatchdogRoutine()
        {
            int state;

            while (!_cts.IsCancellationRequested)
            {
                state = EcHL.ReadState(this.Context);

                if (state < 8)
                {
                    _statusCheckFailedCounter++;

                    if (_statusCheckFailedCounter >= _settings.MaxRetries)
                    {
                        try
                        {
                            lock (_lock)
                            {
                                _isReconfiguring = true;
                                _logger.LogInformation("reconfiguration started");
                            }

                            this.Configure();
                            _logger.LogInformation("reconfiguration successful");

                            _isReconfiguring = false;
                        }
                        catch (Exception)
                        {
                            _logger.LogWarning("reconfiguration failed");
                        }
                        finally
                        {
                            _statusCheckFailedCounter = 0;
                        }
                    }
                }
                else
                {
                    if (_isReconfiguring)
                    {
                        _logger.LogInformation("communication restored");
                        _isReconfiguring = false;
                    }

                    _statusCheckFailedCounter = 0;
                }

                _cts.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(_settings.WatchdogSleepTime));
            }
        }

        #region IDisposable Support

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (_ioMapPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(_ioMapPtr);
                }

                _cts?.Cancel();

                try
                {
                    _watchdogTask?.Wait();
                }
                catch (Exception ex) when (ex.InnerException.GetType() == typeof(TaskCanceledException))
                {
                    //
                }

                if (this.Context != IntPtr.Zero)
                {
                    EcHL.FreeContext(this.Context);
                    this.Context = IntPtr.Zero;
                }

                disposedValue = true;
            }
        }

        ~EcMaster()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
