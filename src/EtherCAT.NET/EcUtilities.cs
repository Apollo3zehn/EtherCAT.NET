using EtherCAT.NET.Extension;
using EtherCAT.NET.Infrastructure;
using SOEM.PInvoke;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace EtherCAT.NET
{
    public static class EcUtilities
    {
        private static Random _random;

        #region "Constructors"

        static EcUtilities()
        {
            _random = new Random();
        }

        #endregion

        #region "Methods"

        public static byte GetBitLength(EthercatDataType ethercatDataType)
        {
            switch (ethercatDataType)
            {
                case EthercatDataType.Boolean:
                    return 1;

                case EthercatDataType.Unsigned8:
                    return 8;

                case EthercatDataType.Integer8:
                    return 8;

                case EthercatDataType.Unsigned16:
                    return 16;

                case EthercatDataType.Integer16:
                    return 16;

                case EthercatDataType.Unsigned32:
                    return 32;

                case EthercatDataType.Integer32:
                    return 32;

                case EthercatDataType.Unsigned64:
                    return 64;

                case EthercatDataType.Integer64:
                    return 64;

                case EthercatDataType.Float32:
                    return 32;

                case EthercatDataType.Float64:
                    return 64;

                default: // other data types are currently not supported - could return 0 cause IO map issues?
                    return 0;
            }
        }


        public static EthercatDataType ParseEtherCatDataType(string value)
        {
            if (value == null)
                return 0;
            else
                return (EthercatDataType)Enum.Parse(typeof(EthercatDataType), value);
        }

        public static IEnumerable<T> TrueDistinct<T>(this IEnumerable<T> inputs)
        {
            return inputs.GroupBy(x => x).Where(x => x.Count() == 1).SelectMany(x => x);
        }

        public static byte[] ToByteArray(this object value)
        {
            int rawSize;
            byte[] rawData;
            GCHandle gcHandle;

            rawSize = Marshal.SizeOf(value);
            rawData = new byte[rawSize];
            gcHandle = GCHandle.Alloc(rawData, GCHandleType.Pinned);

            Marshal.StructureToPtr(value, gcHandle.AddrOfPinnedObject(), false);
            gcHandle.Free();

            return rawData.ToArray();
        }

        /// <summary>
        /// Checks if unmanaged status code represents an error. If true this error will be thrown.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <param name="callerMemberName">The name of the calling function.</param>
        public static void CheckErrorCode(IntPtr context, int errorCode, [CallerMemberName()] string callerMemberName = "")
        {
            if (errorCode <= 0)
            {
                errorCode = -errorCode;

                // message_managed
                var message_managed = ErrorMessage.ResourceManager.GetString($"Native_0x{ errorCode.ToString("X4") }");

                if (string.IsNullOrWhiteSpace(message_managed))
                    message_managed = ErrorMessage.Native_0xFFFF;

                // message_SOEM
                var message_SOEM = string.Empty;

                while (EcHL.HasEcError(context))
                {
                    if (!string.IsNullOrWhiteSpace(message_SOEM))
                        message_SOEM += "\n";

                    message_SOEM += Marshal.PtrToStringAnsi(EcHL.GetNextError(context));
                }

                // message_combined
                var message_combined = $"{ callerMemberName } failed (0x{ errorCode.ToString("X4") }): { message_managed }";

                if (!string.IsNullOrWhiteSpace(message_SOEM))
                    message_combined += $"\n\nEtherCAT message:\n\n{ message_SOEM }";

                throw new Exception(message_combined);
            }
        }

        public static Dictionary<string, string> GetAvailableNetworkInterfaces()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(x => x.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                .ToDictionary(x => x.Description, x => x.GetPhysicalAddress()
                .ToString());
        }

        public static void CreateDynamicData(string esiDirectoryPath, SlaveInfo slave)
        {
            // find ESI
            if (slave.Csa != 0)
                (slave.Esi, slave.EsiGroup) = EsiUtilities.FindEsi(esiDirectoryPath, slave.Manufacturer, slave.ProductCode, slave.Revision);

            //
            var pdos = new List<SlavePdo>();
            var base64ImageData = new byte[] { };

            var name = slave.Esi.Type.Value;
            var description = slave.Esi.Name.FirstOrDefault()?.Value;

            if (description.StartsWith(name))
                description = description.Substring(name.Length);

            else if (string.IsNullOrWhiteSpace(description))
                description = "no description available";

            // PDOs
            foreach (DataDirection dataDirection in Enum.GetValues(typeof(DataDirection)))
            {
                IEnumerable<PdoType> pdoTypes = null;

                switch (dataDirection)
                {
                    case DataDirection.Output:
                        pdoTypes = slave.Esi.RxPdo;
                        break;
                    case DataDirection.Input:
                        pdoTypes = slave.Esi.TxPdo;
                        break;
                }

                foreach (var pdoType in pdoTypes)
                {
                    var osMax = Convert.ToUInt16(pdoType.OSMax);

                    if (osMax == 0)
                    {
                        var pdoName = pdoType.Name.First().Value;
                        var pdoIndex = (ushort)EsiUtilities.ParseHexDecString(pdoType.Index.Value);
                        var syncManager = pdoType.SmSpecified ? pdoType.Sm : -1;

                        var slavePdo = new SlavePdo(slave, pdoName, pdoIndex, osMax, pdoType.Fixed, pdoType.Mandatory, syncManager);

                        pdos.Add(slavePdo);

                        var slaveVariables = pdoType.Entry.Select(x =>
                        {
                            var variableIndex = (ushort)EsiUtilities.ParseHexDecString(x.Index.Value);
                            var subIndex = Convert.ToByte(x.SubIndex);
                            //// Improve. What about -1 if SubIndex does not exist?
                            return new SlaveVariable(slavePdo, x.Name?.FirstOrDefault()?.Value, variableIndex, subIndex, dataDirection, EcUtilities.ParseEtherCatDataType(x.DataType?.Value), (byte)x.BitLen);
                        }).ToList();

                        slavePdo.SetVariables(slaveVariables);
                    }
                    else
                    {
                        for (ushort indexOffset = 0; indexOffset <= osMax - 1; indexOffset++)
                        {
                            var pdoName = $"{pdoType.Name.First().Value} [{indexOffset}]";
                            var pdoIndex = (ushort)((ushort)EsiUtilities.ParseHexDecString(pdoType.Index.Value) + indexOffset);
                            var syncManager = pdoType.SmSpecified ? pdoType.Sm : -1;
                            var indexOffset_Tmp = indexOffset;

                            var slavePdo = new SlavePdo(slave, pdoName, pdoIndex, osMax, pdoType.Fixed, pdoType.Mandatory, syncManager);

                            pdos.Add(slavePdo);

                            var slaveVariables = pdoType.Entry.Select(x =>
                            {
                                var variableIndex = (ushort)EsiUtilities.ParseHexDecString(x.Index.Value);
                                var subIndex = (byte)(byte.Parse(x.SubIndex) + indexOffset_Tmp);
                                //// Improve. What about -1 if SubIndex does not exist?
                                return new SlaveVariable(slavePdo, x.Name.FirstOrDefault()?.Value, variableIndex, subIndex, dataDirection, EcUtilities.ParseEtherCatDataType(x.DataType?.Value), (byte)x.BitLen);
                            }).ToList();

                            slavePdo.SetVariables(slaveVariables);
                        }
                    }
                }
            }

            // image data
            if (slave.EsiGroup.ItemElementName == ItemChoiceType1.ImageData16x14)
                base64ImageData = (byte[])slave.EsiGroup.Item;

            if (slave.Esi.ItemElementName.ToString() == nameof(ItemChoiceType1.ImageData16x14))
                base64ImageData = (byte[])slave.Esi.Item;

            // attach dynamic data
            slave.DynamicData = new SlaveInfoDynamicData(name, description, pdos, base64ImageData);

            // add DC extension to extensions
            if (slave.Esi.Dc is not null
            && !slave.Extensions.Any(extension => extension.GetType() == typeof(DistributedClocksExtension)))
            {
                slave.Extensions.Add(new DistributedClocksExtension(slave));
            }

            // execute extension logic
            slave.Extensions.ToList().ForEach(extension =>
            {
                extension.EvaluateSettings();
            });
        }

        public static SlaveInfo RescanDevices(EcSettings settings, SlaveInfo referenceRootSlave)
        {
            var referenceSlaves = default(IEnumerable<SlaveInfo>);

            if (referenceRootSlave != null)
                referenceSlaves = referenceRootSlave.Descendants().ToList();

            var newRootSlave = EcUtilities.ScanDevices(settings.InterfaceName, referenceRootSlave);

            newRootSlave.Descendants().ToList().ForEach(slave =>
            {
                var referenceSlave = slave.Csa == slave.OldCsa
                    ? referenceSlaves?.FirstOrDefault(x => x.Csa == slave.Csa)
                    : null;

                if (referenceSlave != null)
                    slave.Extensions = referenceSlave.Extensions;

                EcUtilities.CreateDynamicData(settings.EsiDirectoryPath, slave);
            });

            return newRootSlave;
        }

        public static SlaveInfo ScanDevices(string interfaceName, SlaveInfo referenceRootSlave = null)
        {
            var nic = NetworkInterface.GetAllNetworkInterfaces().Where(x => x.Name == interfaceName).FirstOrDefault();

            if (nic is null)
                throw new Exception($"The network interface '{interfaceName}' could not be found.");

            if (nic.OperationalStatus != OperationalStatus.Up)
                throw new Exception($"The network interface '{interfaceName}' is not linked. Aborting action.");

            var context = EcHL.CreateContext();
            var rootSlave = EcUtilities.ScanDevices(context, interfaceName, referenceRootSlave);
            EcHL.FreeContext(context);

            return rootSlave;
        }

        /// <summary>
        /// Initializes EtherCAT and returns found slaves. 
        /// </summary>
        /// <param name="interfaceName">The name of the network adapter.</param>
        /// <returns>Returns found slave.</returns>
        public static SlaveInfo ScanDevices(IntPtr context, string interfaceName, SlaveInfo referenceSlave = null)
        {
            ec_slave_info_t[] refSlaveIdentifications = null;

            if (referenceSlave != null)
                refSlaveIdentifications = EcUtilities.ToSlaveIdentifications(referenceSlave);
            else
                refSlaveIdentifications = new ec_slave_info_t[] { };

            // scan devices
            var networkInterface = NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(nic => nic.Name == interfaceName)
                .FirstOrDefault();

            if (networkInterface == null)
                throw new Exception($"{ ErrorMessage.SoemWrapper_NetworkInterfaceNotFound } Interface name: '{ interfaceName }'.");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                interfaceName = $@"rpcap://\Device\NPF_{networkInterface.Id}";

            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                     RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                interfaceName = $"{interfaceName}";

            else
                throw new PlatformNotSupportedException();

            EcUtilities.CheckErrorCode(context, EcHL.ScanDevices(context, interfaceName, out var slaveIdentifications, out var slaveCount));

            // create slaveInfo from received data
            var offset = 0;
            var newSlaveIdentifications = new ec_slave_info_t[slaveCount + 1]; // correct because EC master = slaveIdentifications[0]

            for (int i = 0; i <= newSlaveIdentifications.Count() - 1; i++)
            {
                newSlaveIdentifications[i] = Marshal.PtrToStructure<ec_slave_info_t>(IntPtr.Add(slaveIdentifications, offset));
                offset += Marshal.SizeOf(typeof(ec_slave_info_t));
            }

            // validate CSA
            while (EcUtilities.EnsureValidCsa(context, newSlaveIdentifications, refSlaveIdentifications))
            {
                //
            }

            return EcUtilities.ToSlaveInfo(newSlaveIdentifications);
        }

        public static SlavePdo[] UploadPdoConfig(IntPtr context, UInt16 slave, UInt16 smIndex)
        {
            EcHL.GetSyncManagerType(context, slave, smIndex, out var syncManagerType);

            DataDirection dataDirection;

            switch (syncManagerType)
            {
                case SyncManagerType.Inputs:
                    dataDirection = DataDirection.Input;
                    break;

                case SyncManagerType.Outputs:
                    dataDirection = DataDirection.Output;
                    break;

                default:
                    throw new ArgumentException();
            }

            EcHL.UploadPdoConfig(context, slave, smIndex, out var ecPdoInfoPtrs, out var pdoCount);

            var slavePdos = Enumerable.Range(0, pdoCount).Select(index =>
            {
                var ecPdoInfoPtr = IntPtr.Add(ecPdoInfoPtrs, index * Marshal.SizeOf(typeof(ec_pdo_info_t)));
                var ecPdoInfo = Marshal.PtrToStructure<ec_pdo_info_t>(ecPdoInfoPtr);
                var slavePdo = new SlavePdo(null, ecPdoInfo.name, ecPdoInfo.index, 0, true, true, smIndex - 0x1C10);

                slavePdo.SetVariables(Enumerable.Range(0, ecPdoInfo.variableCount).Select(index2 =>
                {
                    var ecVariableInfoPtr = IntPtr.Add(ecPdoInfo.variableInfos, index2 * Marshal.SizeOf(typeof(ec_variable_info_t)));
                    var ecVariableInfo = Marshal.PtrToStructure<ec_variable_info_t>(ecVariableInfoPtr);
                    var slaveVariable = new SlaveVariable(slavePdo, ecVariableInfo.name, ecVariableInfo.index, ecVariableInfo.subIndex, dataDirection, ecVariableInfo.dataType);

                    return slaveVariable;
                }).ToList());

                EcHL.Free(ecPdoInfo.variableInfos);

                return slavePdo;
            }).ToArray();

            EcHL.Free(ecPdoInfoPtrs);

            return slavePdos;
        }

        private static bool EnsureValidCsa(IntPtr context, ec_slave_info_t[] newSlaves, ec_slave_info_t[] referenceSlaves)
        {
            // UPDATE: first step ('isValid') is skipped as CSA is now generated randomly. This function returns now a boolean to indicate success.
            //
            //
            //
            // Idea: 
            // 1. isValid: select all slaves whose CSA is greater than zero but smaller than the CSA-base
            //
            // 2. hasUniqueCsa: select all slaves with overall unique CSA (both lists)
            //
            // 3. toBeProtected: select all slaves of new list that are distinct (comparison of CSA is not sufficient) and have counterpart in reference list
            //    -> throw out all slaves where reference CSA is not unique
            //
            // 4. Combination: isValid & (hasUniqueCsa | toBeProtected)
            //
            // ## EXAMPLE ##                                                * -> indicates that new slave is identical to referenced slave
            //
            // CSA base             : 7 
            //
            //  reference slave CSAs: 1 1 2 3
            //        new slave CSAs: 1*| 2  2*| 3  3* 3* | 4 | 9 | 0
            // 
            //
            //                hasRef: 1 | 0  1 | 0  1  1  | 0 | 0 | 0
            //            isDistinct: 1 | 1  1 | 1  0  0  | 1 | 1 | 1
            //      hasUniqueCsa_ref: 0 | 1  1 | 1  1  1  | 0 | 0 | 0
            //                        ===============================
            //         toBeProtected: 0 | 0  1 | 0  0  0  | 0 | 0 | 0
            //
            //
            //        new slave CSAs: 1*| 2  2*| 3  3* 3* | 4 | 9 | 0
            //                        _______________________________
            //               isValid: 1 | 1  1 | 1  1  1  | 1 | 0 | 0    
            //          hasUniqueCsa: 0 | 0  1 | 0  0  0  | 1 | 1 | 1
            //         toBeProtected: 0 | 0  1 | 0  0  0  | 0 | 0 | 0
            //                        ===============================
            //                result: 0 | 0  1 | 0  0  0  | 1 | 0 | 0
            //
            // 5. take all slaves whose result is '0' and update their CSA
            //                result: 7 | 8  2 | 9  10 11 | 4 | 12| 13

            //// reference
            //var reference1 = new ec_slave_info_t() { csa = 1, manufacturer = 2, productCode = 2, revision = 2 }; // 1
            //var reference2 = new ec_slave_info_t() { csa = 1, manufacturer = 3, productCode = 2, revision = 2 }; // 1
            //var reference3 = new ec_slave_info_t() { csa = 2, manufacturer = 2, productCode = 2, revision = 2 }; // 2
            //var reference4 = new ec_slave_info_t() { csa = 3, manufacturer = 2, productCode = 2, revision = 2 }; // 3
            //var reference = new ec_slave_info_t[] { reference1, reference2, reference3, reference4 };

            //// new
            //var new1 = new ec_slave_info_t() { csa = 1, manufacturer = 2, productCode = 2, revision = 2 }; // 1
            //var new2 = new ec_slave_info_t() { csa = 2, manufacturer = 2, productCode = 3, revision = 2 }; // 2
            //var new3 = new ec_slave_info_t() { csa = 2, manufacturer = 2, productCode = 2, revision = 2 }; // 2
            //var new4 = new ec_slave_info_t() { csa = 3, manufacturer = 7, productCode = 2, revision = 2 }; // 3
            //var new5 = new ec_slave_info_t() { csa = 3, manufacturer = 2, productCode = 2, revision = 2 }; // 3
            //var new6 = new ec_slave_info_t() { csa = 3, manufacturer = 2, productCode = 2, revision = 2 }; // 3
            //var new7 = new ec_slave_info_t() { csa = 4, manufacturer = 2, productCode = 2, revision = 2 }; // 4
            //var new8 = new ec_slave_info_t() { csa = 9, manufacturer = 2, productCode = 2, revision = 2 }; // 9
            //var new9 = new ec_slave_info_t() { csa = 0, manufacturer = 2, productCode = 2, revision = 2 }; // 0
            //var newset = new ec_slave_info_t[] { new1, new2, new3, new4, new5, new6, new7, new8, new9 };

            //Settings.Default.CsaBase = 7;

            //SoemWrapper.ValidateCsa(newset, reference);

            var totalSlaves = newSlaves.Concat(referenceSlaves).ToArray();
            var totalSlavesCsaDistinct = totalSlaves.Select(x => x.csa).TrueDistinct().ToArray();
            var newSlavesDistinct = newSlaves.TrueDistinct().ToArray();
            var referenceSlavesCsaDistinct = referenceSlaves.Select(x => x.csa).TrueDistinct().ToArray();
            var hasCsaChanged = false;

            for (int i = 0; i < newSlaves.Count(); i++)
            {
                ec_slave_info_t newSlave = newSlaves[i];

                // 1. - isValid
                var isValid = true; // isValid = newSlave.csa > 0 && newSlave.csa < originalCsaBase;

                // 2. - hasUniqueCsa
                var hasUniqueCsa = totalSlavesCsaDistinct.Contains(newSlave.csa);

                // 3. - toBeProtected

                // => hasRef
                var referenceSlave = referenceSlaves.FirstOrDefault(x => x.csa == newSlave.csa);

                var hasRef = referenceSlave.csa == newSlave.csa &&
                             referenceSlave.manufacturer == newSlave.manufacturer &&
                             referenceSlave.productCode == newSlave.productCode &&
                             referenceSlave.revision == newSlave.revision;

                // => isDistinct
                var isDistinct = newSlavesDistinct.Contains(newSlave);

                // => hasUniqueCsa_ref
                var hasUniqueCsa_ref = referenceSlavesCsaDistinct.Contains(newSlave.csa);

                // => toBeProtected 
                var toBeProtected = hasRef && isDistinct && hasUniqueCsa_ref;

                // 4. Combination
                if (i > 0 && !(isValid && (hasUniqueCsa || toBeProtected)))
                {
                    var csaValue = (ushort)_random.Next(1, ushort.MaxValue);
                    EcHL.UpdateCsa(context, newSlaves.ToList().IndexOf(newSlave), csaValue);
                    newSlave.csa = csaValue;
                    newSlaves[i] = newSlave;
                    hasCsaChanged = true;
                }
            }

            return hasCsaChanged;
        }

        private static SlaveInfo ToSlaveInfo(IList<ec_slave_info_t> slaveIdentifications)
        {
            var slaves = new List<SlaveInfo>();

            for (int i = 0; i < slaveIdentifications.Count(); i++)
            {
                SlaveInfo currentSlave;

                currentSlave = new SlaveInfo(slaveIdentifications[i]);

                if (i > 0)
                    slaves[slaveIdentifications[i].parentIndex].Children.Add(currentSlave);

                slaves.Add(currentSlave);
            }

            return slaves.First();
        }

        private static ec_slave_info_t[] ToSlaveIdentifications(SlaveInfo slave)
        {
            var slaveIdentifications = slave.Descendants().ToList().Select(x => new ec_slave_info_t(x)).ToList();
            slaveIdentifications.Insert(0, new ec_slave_info_t(slave));

            return slaveIdentifications.ToArray();
        }

        public static string GetSlaveStateDescription(IntPtr context, IEnumerable<SlaveInfo> slaves)
        {
            var slaveStateDescription = new StringBuilder();

            foreach (var slave in slaves)
            {
                var slaveIndex = Convert.ToUInt16(slaves.ToList().IndexOf(slave) + 1);

                // Since the registers 0x092c and 0x0932 cannot be read always, the value will be reset before
                var systemTimeDifference = int.MaxValue;
                var speedCounterDifference = ushort.MaxValue;

                ushort requestedState = 0;
                ushort actualState = 0;
                ushort alStatusCode = 0;
                ushort outputPdoCount = 0;
                ushort inputPdoCount = 0;

                var returnValue = EcHL.ReadSlaveState(context, slaveIndex, ref requestedState, ref actualState, ref alStatusCode, ref systemTimeDifference, ref speedCounterDifference, ref outputPdoCount, ref inputPdoCount);

                // ActualState <> RequestedState OrElse AlStatusCode > 0x0 Then
                if (true)
                {
                    if (returnValue < 0)
                    {
                        slaveStateDescription.AppendLine($"-- Error reading data ({slave.DynamicData.Name}) --");
                    }
                    else
                    {
                        string hasCompleteAccess = slave.Esi.Mailbox?.CoE?.CompleteAccess == true 
                            ? " True" 
                            : "False";

                        slaveStateDescription.AppendLine($"Slave {slaveIndex,3} | CA: {hasCompleteAccess} | Req-State: 0x{requestedState:X4} | Act-State: 0x{actualState:X4} | AL-Status: 0x{alStatusCode:X4} | Sys-Time Diff: 0x{systemTimeDifference:X8} | Speed Counter Diff: 0x{speedCounterDifference:X4} | #Pdo out: {outputPdoCount} | #Pdo in: {inputPdoCount} | ({slave.DynamicData.Name})");
                    }
                }
            }

            return slaveStateDescription.ToString();
        }

        public static int SdoWrite(IntPtr context, UInt16 slaveIndex, UInt16 sdoIndex, byte sdoSubIndex, IEnumerable<object> dataset)
        {
            return EcHL.SdoWrite(
                context,
                slaveIndex,
                sdoIndex,
                sdoSubIndex,
                dataset.SelectMany(value => value.ToByteArray()).ToArray(),
                Convert.ToUInt32(dataset.Count()),
                dataset.Select(data => Marshal.SizeOf(data)).ToArray()
            );
        }

        public static int SdoRead(IntPtr context, UInt16 slaveIndex, UInt16 sdoIndex, byte sdoSubIndex, ref byte[] dataset)
        {
            return EcHL.NoCaSdoRead(
                context,
                slaveIndex,
                sdoIndex,
                sdoSubIndex,
                dataset
            );
        }       

        #endregion
    }
}