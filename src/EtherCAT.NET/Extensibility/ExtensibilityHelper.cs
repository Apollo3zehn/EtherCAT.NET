using EtherCAT.Extensibility;
using EtherCAT.Extension;
using EtherCAT.Infrastructure;
using OneDas;
using OneDas.Extensibility;
using OneDas.Infrastructure;
using SOEM.PInvoke;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;

namespace EtherCAT.NET.Extensibility
{
    public static class ExtensibilityHelper
    {
        public static void CreateDynamicData(string esiDirectoryPath, IExtensionFactory extensionFactory, SlaveInfo slaveInfo)
        {
            string name;
            string description;
            List<SlavePdo> pdoSet;
            byte[] base64ImageData;

            // find ESI
            if (slaveInfo.Csa != 0)
            {
                (slaveInfo.SlaveEsi, slaveInfo.SlaveEsi_Group) = EsiUtilities.FindEsi(esiDirectoryPath, slaveInfo.Manufacturer, slaveInfo.ProductCode, slaveInfo.Revision);
            }

            //
            pdoSet = new List<SlavePdo>();
            base64ImageData = new byte[] { };

            name = slaveInfo.SlaveEsi.Type.Value;
            description = slaveInfo.SlaveEsi.Name.FirstOrDefault()?.Value;

            if (description.StartsWith(name))
            {
                description = description.Substring(name.Length);
            }
            else if (string.IsNullOrWhiteSpace(description))
            {
                description = "no description available";
            }

            // PDOs
            foreach (DataDirection dataDirection in Enum.GetValues(typeof(DataDirection)))
            {
                IEnumerable<PdoType> pdoTypeSet = null;

                switch (dataDirection)
                {
                    case DataDirection.Output:
                        pdoTypeSet = slaveInfo.SlaveEsi.RxPdo;
                        break;
                    case DataDirection.Input:
                        pdoTypeSet = slaveInfo.SlaveEsi.TxPdo;
                        break;
                }

                foreach (PdoType pdoType in pdoTypeSet)
                {
                    int syncManager;

                    ushort osMax;
                    ushort pdoIndex;
                    ushort indexOffset_Tmp;

                    string pdoName;

                    SlavePdo slavePdo;

                    osMax = Convert.ToUInt16(pdoType.OSMax);

                    if (osMax == 0)
                    {
                        pdoName = pdoType.Name.First().Value;
                        pdoIndex = ushort.Parse(pdoType.Index.Value.Substring(2), NumberStyles.HexNumber);
                        syncManager = pdoType.SmSpecified ? pdoType.Sm : -1;

                        slavePdo = new SlavePdo(slaveInfo, pdoName, pdoIndex, osMax, pdoType.Fixed, pdoType.Mandatory, syncManager);

                        pdoSet.Add(slavePdo);

                        IList<SlaveVariable> slaveVariableSet = pdoType.Entry.Select(x =>
                        {
                            ushort variableIndex = ushort.Parse(x.Index.Value.Substring(2), NumberStyles.HexNumber);
                            byte subIndex = Convert.ToByte(Convert.ToByte(x.SubIndex));
                            //// Improve. What about -1 if SubIndex does not exist?
                            return new SlaveVariable(slavePdo, x.Name?.FirstOrDefault()?.Value, variableIndex, subIndex, dataDirection, EcUtilities.GetOneDasDataTypeFromEthercatDataType(x.DataType?.Value), (byte)x.BitLen);
                        }).ToList();

                        slavePdo.SetVariableSet(slaveVariableSet);
                    }
                    else
                    {
                        for (ushort indexOffset = 0; indexOffset <= osMax - 1; indexOffset++)
                        {
                            pdoName = $"{pdoType.Name.First().Value} [{indexOffset}]";
                            pdoIndex = (ushort)(ushort.Parse(pdoType.Index.Value.Substring(2), NumberStyles.HexNumber) + indexOffset);
                            syncManager = pdoType.SmSpecified ? pdoType.Sm : -1;
                            indexOffset_Tmp = indexOffset;

                            slavePdo = new SlavePdo(slaveInfo, pdoName, pdoIndex, osMax, pdoType.Fixed, pdoType.Mandatory, syncManager);

                            pdoSet.Add(slavePdo);

                            IList<SlaveVariable> slaveVariableSet = pdoType.Entry.Select(x =>
                            {
                                ushort variableIndex = ushort.Parse(x.Index.Value.Substring(2), NumberStyles.HexNumber);
                                byte subIndex = Convert.ToByte(Convert.ToByte(x.SubIndex) + indexOffset_Tmp);
                                //// Improve. What about -1 if SubIndex does not exist?
                                return new SlaveVariable(slavePdo, x.Name.FirstOrDefault()?.Value, variableIndex, subIndex, dataDirection, EcUtilities.GetOneDasDataTypeFromEthercatDataType(x.DataType?.Value), (byte)x.BitLen);
                            }).ToList();

                            slavePdo.SetVariableSet(slaveVariableSet);
                        }
                    }
                }
            }

            // image data
            if (slaveInfo.SlaveEsi_Group.ItemElementName == ItemChoiceType1.ImageData16x14)
            {
                base64ImageData = (byte[])slaveInfo.SlaveEsi_Group.Item;
            }

            if (slaveInfo.SlaveEsi.ItemElementName.ToString() == nameof(ItemChoiceType1.ImageData16x14))
            {
                base64ImageData = (byte[])slaveInfo.SlaveEsi.Item;
            }

            // attach dynamic data
            slaveInfo.DynamicData = new SlaveInfoDynamicData(name, description, pdoSet, base64ImageData);

            // execute extension logic
            ExtensibilityHelper.UpdateSlaveExtensions(extensionFactory, slaveInfo);

            slaveInfo.SlaveExtensionSet.ToList().ForEach(slaveExtension =>
            {
                slaveExtension.EvaluateSettings();
            });
        }

        public static void UpdateSlaveExtensions(IExtensionFactory extensionFactory, SlaveInfo slaveInfo, SlaveInfo referenceEcSlaveInfo = null)
        {
            IEnumerable<Type> referenceTypeSet;
            IEnumerable<SlaveExtensionSettingsBase> referenceSet;
            IEnumerable<SlaveExtensionSettingsBase> newSet;
            TargetSlaveAttribute targetSlaveAttribute;

            referenceSet = referenceEcSlaveInfo?.SlaveExtensionSet;

            if (referenceSet == null)
            {
                referenceSet = slaveInfo.SlaveExtensionSet;
            }

            referenceTypeSet = referenceSet.Select(x => x.GetType()).ToList();

            newSet = extensionFactory.Get<SlaveExtensionSettingsBase>().Where(type =>
            {
                if (!referenceTypeSet.Contains(type))
                {
                    if (type == typeof(DistributedClocksSettings))
                    {
                        return slaveInfo.SlaveEsi.Dc != null;
                    }
                    else
                    {
                        targetSlaveAttribute = type.GetFirstAttribute<TargetSlaveAttribute>();

                        return targetSlaveAttribute.Manufacturer == slaveInfo.Manufacturer && targetSlaveAttribute.ProductCode == slaveInfo.ProductCode;
                    }
                }
                else
                {
                    return false;
                }
            }).Select(type => (SlaveExtensionSettingsBase)Activator.CreateInstance(type, slaveInfo)).ToList();

            slaveInfo.SlaveExtensionSet = referenceSet.Concat(newSet).ToList();

            // Improve: only required after deserialization, not after construction as the slaveInfo is passed a constructor parameter
            slaveInfo.SlaveExtensionSet.ToList().ForEach(slaveExtension => slaveExtension.SlaveInfo = slaveInfo);
        }

        public static SlaveInfo ReloadHardware(string esiDirectoryPath, IExtensionFactory extensionFactory, string interfaceName, SlaveInfo referenceRootSlaveInfo)
        {
            IntPtr context;
            SlaveInfo newRootSlaveInfo;
            SlaveInfo referenceSlaveInfo;
            IEnumerable<SlaveInfo> referenceSlaveInfoSet;

            referenceSlaveInfo = null;
            referenceSlaveInfoSet = null;

            if (NetworkInterface.GetAllNetworkInterfaces().Where(x => x.GetPhysicalAddress().ToString() == interfaceName).FirstOrDefault()?.OperationalStatus != OperationalStatus.Up)
            {
                throw new Exception($"The network interface '{interfaceName}' is not linked. Aborting action.");
            }

            context = EcHL.CreateContext();
            newRootSlaveInfo = EcUtilities.ScanDevices(context, interfaceName, referenceRootSlaveInfo);
            EcHL.FreeContext(context);

            if (referenceRootSlaveInfo != null)
            {
                referenceSlaveInfoSet = referenceRootSlaveInfo.Descendants().ToList();
            }

            newRootSlaveInfo.Descendants().ToList().ForEach(slaveInfo =>
            {
                referenceSlaveInfo = slaveInfo.Csa == slaveInfo.OldCsa ? referenceSlaveInfoSet?.FirstOrDefault(x => x.Csa == slaveInfo.Csa) : null;
                ExtensibilityHelper.GetDynamicSlaveInfoData(esiDirectoryPath, extensionFactory, slaveInfo);
                ExtensibilityHelper.UpdateSlaveExtensions(extensionFactory, slaveInfo, referenceSlaveInfo);
            });

            return newRootSlaveInfo;
        }

        public static SlaveInfoDynamicData GetDynamicSlaveInfoData(string esiDirectoryPath, IExtensionFactory extensionFactory, SlaveInfo slaveInfo)
        {
            if (slaveInfo.Csa > 0)
            {
                ExtensibilityHelper.CreateDynamicData(esiDirectoryPath, extensionFactory, slaveInfo);

                return slaveInfo.DynamicData;
            }
            else
            {
                return new SlaveInfoDynamicData("EtherCAT Master", "EtherCAT Master", new List<SlavePdo>(), new byte[] { });
            }
        }
    }
}
