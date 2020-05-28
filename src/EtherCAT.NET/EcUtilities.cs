using EtherCAT.NET.Infrastructure;
using OneDas.Infrastructure;
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

        public static OneDasDataType GetOneDasDataTypeFromEthercatDataType(string value)
        {
            if (value == null)
                return 0;
            else
                return EcUtilities.GetOneDasDataTypeFromEthercatDataType((EthercatDataType)Enum.Parse(typeof(EthercatDataType), value));
        }

        public static OneDasDataType GetOneDasDataTypeFromEthercatDataType(EthercatDataType ethercatDataType)
        {
            switch (ethercatDataType)
            {
                case EthercatDataType.Boolean:
                    return OneDasDataType.BOOLEAN;

                case EthercatDataType.Unsigned8:
                    return OneDasDataType.UINT8;

                case EthercatDataType.Integer8:
                    return OneDasDataType.INT8;

                case EthercatDataType.Unsigned16:
                    return OneDasDataType.UINT16;

                case EthercatDataType.Integer16:
                    return OneDasDataType.INT16;

                case EthercatDataType.Unsigned32:
                    return OneDasDataType.UINT32;

                case EthercatDataType.Integer32:
                    return OneDasDataType.INT32;

                case EthercatDataType.Unsigned64:
                    return OneDasDataType.UINT64;

                case EthercatDataType.Integer64:
                    return OneDasDataType.INT64;

                case EthercatDataType.Float32:
                    return OneDasDataType.FLOAT32;

                case EthercatDataType.Float64:
                    return OneDasDataType.FLOAT64;

                default:
                    return 0;
            }
        }

        public static IEnumerable<T> TrueDistinct<T>(this IEnumerable<T> inputSet)
        {
            return inputSet.GroupBy(x => x).Where(x => x.Count() == 1).SelectMany(x => x);
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
                string message_server;
                string message_SOEM = string.Empty;
                string message_combined = string.Empty;

                errorCode = -errorCode;

                // message_server
                message_server = ErrorMessage.ResourceManager.GetString($"Native_0x{ errorCode.ToString("X4") }");

                if (string.IsNullOrWhiteSpace(message_server))
                {
                    message_server = ErrorMessage.Native_0xFFFF;
                }

                // message_SOEM
                while (EcHL.HasEcError(context))
                {
                    if (!string.IsNullOrWhiteSpace(message_SOEM))
                    {
                        message_SOEM += "\n";
                    }

                    message_SOEM += Marshal.PtrToStringAnsi(EcHL.GetNextError(context));
                }

                // message_combined
                message_combined = $"{ callerMemberName } failed (0x{ errorCode.ToString("X4") }): { message_server }";

                if (!string.IsNullOrWhiteSpace(message_SOEM))
                {
                    message_combined += $"\n\nEtherCAT message:\n\n{ message_SOEM }";
                }

                throw new Exception(message_combined);
            }
        }

        public static Dictionary<string, string> GetAvailableNetworkInterfaces()
        {
            return NetworkInterface.GetAllNetworkInterfaces().Where(x => x.NetworkInterfaceType == NetworkInterfaceType.Ethernet).ToDictionary(x => x.Description, x => x.GetPhysicalAddress().ToString());
        }

        public static SlaveInfo ScanDevices(string interfaceName, SlaveInfo referenceSlaveInfo = null)
        {
            IntPtr context;
            SlaveInfo slaveInfo;

            context = EcHL.CreateContext();
            slaveInfo = EcUtilities.ScanDevices(context, interfaceName, referenceSlaveInfo);
            EcHL.FreeContext(context);

            return slaveInfo;
        }

        /// <summary>
        /// Initializes EtherCAT and returns found slaves. 
        /// </summary>
        /// <param name="interfaceName">The name of the network adapter.</param>
        /// <returns>Returns found slave.</returns>
        public static SlaveInfo ScanDevices(IntPtr context, string interfaceName, SlaveInfo referenceSlaveInfo = null)
        {
            int offset;
            int slaveCount;

            NetworkInterface networkInterface;
            ec_slave_info_t[] newSlaveIdentificationSet;
            ec_slave_info_t[] refSlaveIdentificationSet;
            IntPtr slaveIdentificationSet;

            //
            offset = 0;

            networkInterface = NetworkInterface.GetAllNetworkInterfaces().Where(nic => nic.Name == interfaceName).FirstOrDefault();

            newSlaveIdentificationSet = null;

            if (referenceSlaveInfo != null)
            {
                refSlaveIdentificationSet = EcUtilities.ToSlaveIdentificationSet(referenceSlaveInfo);
            }
            else
            {
                refSlaveIdentificationSet = new ec_slave_info_t[] { };
            }

            // scan devices
            if (networkInterface == null)
            {
                throw new Exception($"{ ErrorMessage.SoemWrapper_NetworkInterfaceNotFound } Interface name: '{ interfaceName }'.");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                interfaceName = $@"rpcap://\Device\NPF_{networkInterface.Id}";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                interfaceName = $"{interfaceName}";
            else
                throw new PlatformNotSupportedException();

            EcUtilities.CheckErrorCode(context, EcHL.ScanDevices(context, interfaceName, out slaveIdentificationSet, out slaveCount));

            // create slaveInfo from received data
            newSlaveIdentificationSet = new ec_slave_info_t[slaveCount + 1]; // correct because EC master = slaveIdentificationSet[0]

            for (int i = 0; i <= newSlaveIdentificationSet.Count() - 1; i++)
            {
                newSlaveIdentificationSet[i] = Marshal.PtrToStructure<ec_slave_info_t>(IntPtr.Add(slaveIdentificationSet, offset));
                offset += Marshal.SizeOf(typeof(ec_slave_info_t));
            }

            // validate CSA
            while (EcUtilities.EnsureValidCsa(context, newSlaveIdentificationSet, refSlaveIdentificationSet))
            {
                //
            }

            return EcUtilities.ToSlaveInfo(newSlaveIdentificationSet);
        }

        public static SlavePdo[] UploadPdoConfig(IntPtr context, UInt16 slave, UInt16 smIndex)
        {
            int pdoCount;
            IntPtr ecPdoInfoPtrSet;
            SlavePdo[] slavePdoSet;
            SyncManagerType syncManagerType;
            DataDirection dataDirection;

            //
            EcHL.GetSyncManagerType(context, slave, smIndex, out syncManagerType);

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

            EcHL.UploadPdoConfig(context, slave, smIndex, out ecPdoInfoPtrSet, out pdoCount);

            slavePdoSet = Enumerable.Range(0, pdoCount).Select(index =>
            {
                ec_pdo_info_t ecPdoInfo;
                SlavePdo slavePdo;
                IntPtr ecPdoInfoPtr;

                ecPdoInfoPtr = IntPtr.Add(ecPdoInfoPtrSet, index * Marshal.SizeOf(typeof(ec_pdo_info_t)));
                ecPdoInfo = Marshal.PtrToStructure<ec_pdo_info_t>(ecPdoInfoPtr);
                slavePdo = new SlavePdo(null, ecPdoInfo.name, ecPdoInfo.index, 0, true, true, smIndex - 0x1C10);

                slavePdo.SetVariableSet(Enumerable.Range(0, ecPdoInfo.variableCount).Select(index2 =>
                {
                    ec_variable_info_t ecVariableInfo;
                    SlaveVariable slaveVariable;
                    IntPtr ecVariableInfoPtr;

                    ecVariableInfoPtr = IntPtr.Add(ecPdoInfo.variableInfoSet, index2 * Marshal.SizeOf(typeof(ec_variable_info_t)));
                    ecVariableInfo = Marshal.PtrToStructure<ec_variable_info_t>(ecVariableInfoPtr);
                    slaveVariable = new SlaveVariable(slavePdo, ecVariableInfo.name, ecVariableInfo.index, ecVariableInfo.subIndex, dataDirection, EcUtilities.GetOneDasDataTypeFromEthercatDataType(ecVariableInfo.dataType));

                    return slaveVariable;
                }).ToList());

                EcHL.Free(ecPdoInfo.variableInfoSet);

                return slavePdo;
            }).ToArray();

            EcHL.Free(ecPdoInfoPtrSet);

            return slavePdoSet;
        }

        private static bool EnsureValidCsa(IntPtr context, ec_slave_info_t[] newSlaveSet, ec_slave_info_t[] referenceSlaveSet)
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

            ec_slave_info_t[] totalSlaveSet;
            ec_slave_info_t[] newSlaveSetDistinct;
            ec_slave_info_t referenceSlave;

            ushort[] totalSlaveSetCsaDistinct;
            ushort[] referenceSlaveSetCsaDistinct;
            ushort csaValue;

            bool isValid;
            bool hasUniqueCsa;
            bool toBeProtected;
            bool hasRef;
            bool isDistinct;
            bool hasUniqueCsa_ref;
            bool hasCsaChanged;

            //
            totalSlaveSet = newSlaveSet.Concat(referenceSlaveSet).ToArray();
            totalSlaveSetCsaDistinct = totalSlaveSet.Select(x => x.csa).TrueDistinct().ToArray();
            newSlaveSetDistinct = newSlaveSet.TrueDistinct().ToArray();
            referenceSlaveSetCsaDistinct = referenceSlaveSet.Select(x => x.csa).TrueDistinct().ToArray();
            hasCsaChanged = false;

            for (int i = 0; i < newSlaveSet.Count(); i++)
            {
                ec_slave_info_t newSlave = newSlaveSet[i];

                // 1. - isValid
                isValid = true; // isValid = newSlave.csa > 0 && newSlave.csa < originalCsaBase;

                // 2. - hasUniqueCsa
                hasUniqueCsa = totalSlaveSetCsaDistinct.Contains(newSlave.csa);

                // 3. - toBeProtected

                // => hasRef
                referenceSlave = referenceSlaveSet.FirstOrDefault(x => x.csa == newSlave.csa);

                hasRef = referenceSlave.csa == newSlave.csa &&
                         referenceSlave.manufacturer == newSlave.manufacturer &&
                         referenceSlave.productCode == newSlave.productCode &&
                         referenceSlave.revision == newSlave.revision;

                // => isDistinct
                isDistinct = newSlaveSetDistinct.Contains(newSlave);

                // => hasUniqueCsa_ref
                hasUniqueCsa_ref = referenceSlaveSetCsaDistinct.Contains(newSlave.csa);

                // => toBeProtected 
                toBeProtected = hasRef && isDistinct && hasUniqueCsa_ref;

                // 4. Combination
                if (i > 0 && !(isValid && (hasUniqueCsa || toBeProtected)))
                {
                    csaValue = (ushort)_random.Next(1, ushort.MaxValue);
                    EcHL.UpdateCsa(context, newSlaveSet.ToList().IndexOf(newSlave), csaValue);
                    newSlave.csa = csaValue;
                    newSlaveSet[i] = newSlave;
                    hasCsaChanged = true;
                }
            }

            return hasCsaChanged;
        }

        private static SlaveInfo ToSlaveInfo(IList<ec_slave_info_t> slaveIdentificationSet)
        {
            List<SlaveInfo> slaveInfoSet;

            slaveInfoSet = new List<SlaveInfo>();

            for (int i = 0; i < slaveIdentificationSet.Count(); i++)
            {
                SlaveInfo currentSlaveInfo;

                currentSlaveInfo = new SlaveInfo(slaveIdentificationSet[i]);

                if (i > 0)
                {
                    slaveInfoSet[slaveIdentificationSet[i].parentIndex].ChildSet.Add(currentSlaveInfo);
                }

                slaveInfoSet.Add(currentSlaveInfo);
            }

            return slaveInfoSet.First();
        }

        private static ec_slave_info_t[] ToSlaveIdentificationSet(SlaveInfo slaveInfo)
        {
            List<ec_slave_info_t> slaveIdentificationSet;

            slaveIdentificationSet = slaveInfo.Descendants().ToList().Select(x => new ec_slave_info_t(x)).ToList();
            slaveIdentificationSet.Insert(0, new ec_slave_info_t(slaveInfo));

            return slaveIdentificationSet.ToArray();
        }

        /// <summary>
        /// Gathers information about the current status of the requested list of <see cref="SlaveInfo"/>.
        /// </summary>
        /// <param name="SlaveInfoSet">The list of <see cref="SlaveInfo"/>.</param>
        /// <returns>Returns information about the current status of the requested list of <see cref="SlaveInfo"/>.</returns>
        public static string GetSlaveStateDescription(IntPtr context, IEnumerable<SlaveInfo> slaveInfoSet)
        {
            StringBuilder slaveStateDescription = new StringBuilder();
            ushort slaveIndex = 0;
            ushort requestedState = 0;
            ushort actualState = 0;
            ushort alStatusCode = 0;
            ushort speedCounterDifference = 0;
            ushort outputPdoCount = 0;
            ushort inputPdoCount = 0;
            int systemTimeDifference = 0;
            int returnValue = 0;

            foreach (SlaveInfo slaveInfo in slaveInfoSet)
            {
                slaveIndex = Convert.ToUInt16(slaveInfoSet.ToList().IndexOf(slaveInfo) + 1);

                // Since the registers 0x092c and 0x0932 cannot be read always, the value will be reset before
                systemTimeDifference = int.MaxValue;
                speedCounterDifference = ushort.MaxValue;

                returnValue = EcHL.ReadSlaveState(context, slaveIndex, ref requestedState, ref actualState, ref alStatusCode, ref systemTimeDifference, ref speedCounterDifference, ref outputPdoCount, ref inputPdoCount);

                // ActualState <> RequestedState OrElse AlStatusCode > 0x0 Then
                if (true)
                {
                    if (returnValue < 0)
                    {
                        slaveStateDescription.AppendLine($"-- Error reading data ({slaveInfo.DynamicData.Name}) --");
                    }
                    else
                    {
                        string hasCompleteAccess = slaveInfo.SlaveEsi.Mailbox?.CoE?.CompleteAccess == true ? " True" : "False";
                        slaveStateDescription.AppendLine($"Slave {slaveIndex,3} | CA: {hasCompleteAccess} | Req-State: 0x{requestedState:X4} | Act-State: 0x{actualState:X4} | AL-Status: 0x{alStatusCode:X4} | Sys-Time Diff: 0x{systemTimeDifference:X8} | Speed Counter Diff: 0x{speedCounterDifference:X4} | #Pdo out: {outputPdoCount} | #Pdo in: {inputPdoCount} | ({slaveInfo.DynamicData.Name})");
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

        #endregion
    }
}