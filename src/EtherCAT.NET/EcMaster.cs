﻿using EtherCAT.NET.Extension;
using EtherCAT.NET.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SOEM.PInvoke;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace EtherCAT.NET
{
    public class EcMaster : IDisposable
    {
        #region Fields

        // general
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
        private bool _watchDogActive = true;

        #endregion

        #region Constructors

        public EcMaster(EcSettings settings) 
            : this(settings, NullLogger.Instance)
        {
            //
        }

        public EcMaster(EcSettings settings, ILogger logger)
        {
            _settings = settings.ShallowCopy();
            _logger = logger;

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

        private void ValidateSlaves(IList<SlaveInfo> slaves, IList<SlaveInfo> actualSlaves)
        {
            if (slaves.Count() != actualSlaves.Count())
                throw new Exception(ErrorMessage.EthercatGateway_EtherCATConfigurationMismatch);

            for (int i = 0; i <= actualSlaves.Count() - 1; i++)
            {
                if (!(actualSlaves[i].ProductCode == slaves[i].ProductCode 
                   && actualSlaves[i].Revision == slaves[i].Revision))
                    throw new Exception(ErrorMessage.EthercatGateway_EtherCATConfigurationMismatch);
            }
        }

        private void ConfigureSlaves(IList<SlaveInfo> slaves)
        {
            var callbacks = new List<EcHL.PO2SOCallback>();

            foreach (var slave in slaves)
            {
                // SDO / PDO config / PDO assign
                var currentSlaveIndex = (ushort)(Convert.ToUInt16(slaves.ToList().IndexOf(slave)) + 1);
                var extensions = slave.Extensions;

                var sdoWriteRequests = slave.GetConfiguration(extensions).ToList();

                EcHL.PO2SOCallback callback = slaveIndex =>
                {
                    sdoWriteRequests.ToList().ForEach(sdoWriteRequest =>
                    {
                        EcUtilities.CheckErrorCode(this.Context, EcUtilities.SdoWrite(this.Context, slaveIndex, sdoWriteRequest.Index, sdoWriteRequest.SubIndex, sdoWriteRequest.Dataset), nameof(EcHL.SdoWrite));
                    });

                    return 0;
                };

                EcHL.RegisterCallback(this.Context, currentSlaveIndex, callback);
                callbacks.Add(callback);
            }

            callbacks.ForEach(callback =>
            {
                GC.KeepAlive(callback);
            });
        }

        private void ConfigureIoMap(IList<SlaveInfo> slaves)
        {
            var ioMapByteOffset = 0;
            var ioMapBitOffset = 0;
            var slavePdoOffsets = default(int[]);
            var slaveRxPdoOffsets = new int[slaves.Count() + 1];
            var slaveTxPdoOffsets = new int[slaves.Count() + 1];

            _actualIoMapSize = EcHL.ConfigureIoMap(this.Context, _ioMapPtr, slaveRxPdoOffsets, slaveTxPdoOffsets, out _expectedWorkingCounter);

            foreach (DataDirection dataDirection in Enum.GetValues(typeof(DataDirection)))
            {
                switch (dataDirection)
                {
                    case DataDirection.Output:
                        slavePdoOffsets = slaveRxPdoOffsets;
                        break;
                    case DataDirection.Input:
                        slavePdoOffsets = slaveTxPdoOffsets;
                        break;
                    default:
                        throw new NotImplementedException();
                }

                foreach (var slave in slaves)
                {
                    var slaveByteOffset = slavePdoOffsets[slaves.ToList().IndexOf(slave) + 1];

                    // reset bit offset if byte offset changes
                    if( slaveByteOffset != ioMapByteOffset )
                        ioMapBitOffset = 0;

                    ioMapByteOffset = slaveByteOffset;
                    
                    foreach (var variable in slave.DynamicData.Pdos.Where(x => x.SyncManager >= 0).ToList().SelectMany(x => x.Variables).ToList().Where(x => x.DataDirection == dataDirection))
                    {
                        variable.DataPtr = IntPtr.Add(_ioMapPtr, ioMapByteOffset);
                        variable.BitOffset = ioMapBitOffset;

                        if (variable.DataType == EthercatDataType.Boolean)
                            variable.BitOffset = ioMapBitOffset; // bool is treated as bit-oriented

                        Debug.WriteLine($"{variable.Name} {variable.DataPtr.ToInt64() - _ioMapPtr.ToInt64()}/{variable.BitOffset}");

                        ioMapBitOffset += variable.BitLength;

                        if (ioMapBitOffset > 7)
                        {
                            ioMapBitOffset = ioMapBitOffset % 8;
                            ioMapByteOffset += (variable.BitLength + 7) / 8;
                        }
                    }
                }
            }

            _logger.LogInformation($"IO map configured ({slaves.Count()} {(slaves.Count() > 1 ? "slaves" : "slave")}, {_actualIoMapSize} bytes)");
        }

        private void ConfigureDc()
        {
            EcUtilities.CheckErrorCode(this.Context, EcHL.ConfigureDc(this.Context, _settings.FrameCount, _settings.TargetTimeDifference, out var systemTimeDifference), nameof(EcHL.ConfigureDc));

            _logger.LogInformation($"DC system time diff. is <= {systemTimeDifference & 0x7FFF} ns");
        }

        private void ConfigureSync01(IList<SlaveInfo> slaves)
        {
            // SYNC0 / SYNC1
            foreach (var slave in slaves)
            {
                var slaveIndex = (ushort)(Convert.ToUInt16(slaves.ToList().IndexOf(slave)) + 1);
                var distributedClocksSettings = slave
                    .Extensions
                    .OfType<DistributedClocksExtension>()
                    .ToList()
                    .FirstOrDefault();

                if (distributedClocksSettings != null)
                {
                    if (!slave.Esi.Dc.TimeLoopControlOnly)
                    {
                        byte[] assignActivate = null;
                        var parameters = distributedClocksSettings.CalculateDcParameters(ref assignActivate, _settings.CycleFrequency);
                        EcUtilities.CheckErrorCode(this.Context, EcHL.ConfigureSync01(this.Context, slaveIndex, ref assignActivate, assignActivate.Count(), parameters.CycleTime0, parameters.CycleTime1, parameters.ShiftTime0));
                    }
                }
            }
        }

        public void Configure(SlaveInfo rootSlave = null)
        {
            var networkInterface = NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(nic => nic.Name == _settings.InterfaceName)
                .FirstOrDefault();

            if (networkInterface?.OperationalStatus != OperationalStatus.Up)
                throw new Exception($"Network interface '{_settings.InterfaceName}' is not linked. Aborting action.");

            #region "PreOp"

            var actualSlave = EcUtilities.ScanDevices(this.Context, _settings.InterfaceName, null);

            if (rootSlave == null)
            {
                rootSlave = actualSlave;

                rootSlave.Descendants().ToList().ForEach(current =>
                {
                    EcUtilities.CreateDynamicData(_settings.EsiDirectoryPath, current);
                });
            }

            var slaves = rootSlave.Descendants().ToList();
            var actualSlaves = actualSlave.Descendants().ToList();

            this.ValidateSlaves(slaves, actualSlaves);
            this.ConfigureSlaves(slaves);
            this.ConfigureIoMap(slaves);
            this.ConfigureDc();
            this.ConfigureSync01(slaves);

            #endregion

            #region "SafeOp"

            EcUtilities.CheckErrorCode(this.Context, EcHL.CheckSafeOpState(this.Context), nameof(EcHL.CheckSafeOpState));

            #endregion

            #region "Op"

            EcUtilities.CheckErrorCode(this.Context, EcHL.RequestCommonState(this.Context, (UInt16)SlaveState.Operational), nameof(EcHL.RequestCommonState));

            #endregion

            if (_watchdogTask == null)
                _watchdogTask = Task.Run(() => this.WatchdogRoutine(), _cts.Token);
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
                        //Trace.WriteLine(EcUtilities.GetSlaveStateDescription(_ecSettings.RootSlaves.SelectMany(x => x.Descendants()).ToList()));
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
                    EcHL.CompensateDcDrift(this.Context, Convert.ToInt32(Math.Min(Math.Max(this.DcRingBufferAverage, -_dcDriftCompensationRate), _dcDriftCompensationRate)));

                _dcRingBufferIndex = (_dcRingBufferIndex + 1) % _dcRingBufferSize;
            }
        }

        /// <summary>
        /// Activate watchdog. 
        /// </summary>
        /// <param name="value">True: Watchdog is activated,
        /// false: Watchdog is deactivated.</param>
        public void ActivateWatchdog(bool value)
        {
            _watchDogActive = value;
        }

        /// <summary>
        /// Request slave state transition. 
        /// </summary>
        /// <param name="slaveIndex">Slave index.</param>
        /// <param name="slaveState">Slave target state.</param>
        /// <returns>True if transition was successful, false otherwise./returns>
        public bool RequestState(int slaveIndex, SlaveState slaveState)
        {
            UInt16 stateSet = EcHL.RequestState(this.Context, slaveIndex, (UInt16)slaveState);
            return (SlaveState)stateSet == slaveState;
        }

        /// <summary>
        /// Return current state of slave. 
        /// </summary>
        /// <param name="slaveIndex">Slave index.</param>
        /// <returns>Current slave state./returns>
        public SlaveState GetState(int slaveIndex)
        {
            ushort slaveState = EcHL.GetState(this.Context, slaveIndex);
            return (SlaveState)slaveState;
        }

        /// <summary>
        /// Download firmware file to slave.
        /// 1. All detected slaves are set to PREOP state.
        /// 2. Target slave is set to INIT state.
        /// 3. Target slave is set to BOOT state.
        /// 4. Firmware file is downloaded to target slave.
        /// 5. Target slave is set to INIT state regardless of whether
        /// the file download was successful or not.
        /// </summary>
        /// <param name="slaveIndex">Slave index.</param>
        /// <param name="fileName">Absolute path to firmware file.</param>
        /// <returns>True if operation was successful, false otherwise./returns>
        public bool DownloadFirmware(int slaveIndex, string fileName)
        { 
            FileInfo fileInfo = new FileInfo(fileName);
            if (!fileInfo.Exists)
                return false;

            bool success = false;
            if (EcHL.RequestCommonState(this.Context, (UInt16)SlaveState.PreOp) == 1)
            {
                UInt16 currentState = EcHL.RequestState(this.Context, slaveIndex, (UInt16)SlaveState.Init);
                if (currentState == (UInt16)SlaveState.Init)
                {
                    currentState = EcHL.RequestState(this.Context, slaveIndex, (UInt16)SlaveState.Boot);
                    if (currentState == (UInt16)SlaveState.Boot)
                    {
                        int currentPackageNumber = -1;
                        int totalPackages = 0;
                        int remainingSize = -1;

                        EcHL.FOECallback callback = (slaveIndex, packageNumber,  datasize) =>
                        {
                            if(packageNumber == 0)
                                _logger.LogInformation($"FoE: Write {datasize} bytes to {slaveIndex}. slave");
                            else
                                _logger.LogInformation($"FoE: {packageNumber}. package with {remainingSize - datasize} bytes written to {slaveIndex}. slave. Remaining data: {datasize} bytes");

                            if (currentPackageNumber != packageNumber)
                            {
                                currentPackageNumber = packageNumber;
                                if(packageNumber != 0)
                                    totalPackages++;
                            }

                            remainingSize = datasize;
                            return 0;
                        };

                        EcHL.RegisterFOECallback(this.Context, callback);
                        GC.KeepAlive(callback);

                        int wk = EcHL.DownloadFirmware(this.Context, slaveIndex, fileName, (int)fileInfo.Length);

                        _logger.LogInformation($"FoE: {totalPackages} packages written");
                        success = (remainingSize == 0) && (wk > 0);

                        EcHL.RequestState(this.Context, slaveIndex, (UInt16)SlaveState.Init);
                    }
                }
            }

            return success;
        }


        #endregion

        private void WatchdogRoutine()
        {
            while (!_cts.IsCancellationRequested)
            {
                if (_watchDogActive)
                {
                    var state = EcHL.ReadState(this.Context);

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
                    Marshal.FreeHGlobal(_ioMapPtr);

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
