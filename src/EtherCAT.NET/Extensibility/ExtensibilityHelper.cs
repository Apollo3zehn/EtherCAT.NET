using EtherCAT.NET.Extension;
using EtherCAT.NET.Infrastructure;
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
            List<SlavePdo> pdos;
            byte[] base64ImageData;

            // find ESI
            if (slaveInfo.Csa != 0)
            {
                (slaveInfo.SlaveEsi, slaveInfo.SlaveEsi_Group) = EsiUtilities.FindEsi(esiDirectoryPath, slaveInfo.Manufacturer, slaveInfo.ProductCode, slaveInfo.Revision);
            }

            //
            pdos = new List<SlavePdo>();
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
                IEnumerable<PdoType> pdoTypes = null;

                switch (dataDirection)
                {
                    case DataDirection.Output:
                        pdoTypes = slaveInfo.SlaveEsi.RxPdo;
                        break;
                    case DataDirection.Input:
                        pdoTypes = slaveInfo.SlaveEsi.TxPdo;
                        break;
                }

                foreach (var pdoType in pdoTypes)
                {
                    int syncManager;
                    ushort pdoIndex;
                    string pdoName;
                    SlavePdo slavePdo;

                    var osMax = Convert.ToUInt16(pdoType.OSMax);

                    if (osMax == 0)
                    {
                        pdoName = pdoType.Name.First().Value;
                        pdoIndex = (ushort)EsiUtilities.ParseHexDecString(pdoType.Index.Value);
                        syncManager = pdoType.SmSpecified ? pdoType.Sm : -1;

                        var slavePdo = new SlavePdo(slaveInfo, pdoName, pdoIndex, osMax, pdoType.Fixed, pdoType.Mandatory, syncManager);

                        pdos.Add(slavePdo);

                        var slaveVariables = pdoType.Entry.Select(x =>
                        {
                            var variableIndex = (ushort)EsiUtilities.ParseHexDecString(x.Index.Value);
                            var subIndex = byte.Parse(x.SubIndex);
                            //// Improve. What about -1 if SubIndex does not exist?
                            return new SlaveVariable(slavePdo, x.Name?.FirstOrDefault()?.Value, variableIndex, subIndex, dataDirection, EcUtilities.GetOneDasDataTypeFromEthercatDataType(x.DataType?.Value), (byte)x.BitLen);
                        }).ToList();

                        slavePdo.SetVariables(slaveVariables);
                    }
                    else
                    {
                        for (ushort indexOffset = 0; indexOffset <= osMax - 1; indexOffset++)
                        {
                            pdoName = $"{pdoType.Name.First().Value} [{indexOffset}]";
                            pdoIndex = (ushort)((ushort)EsiUtilities.ParseHexDecString(pdoType.Index.Value) + indexOffset);
                            syncManager = pdoType.SmSpecified ? pdoType.Sm : -1;
                            var indexOffset_Tmp = indexOffset;

                            var slavePdo = new SlavePdo(slaveInfo, pdoName, pdoIndex, osMax, pdoType.Fixed, pdoType.Mandatory, syncManager);

                            pdos.Add(slavePdo);

                            var slaveVariables = pdoType.Entry.Select(x =>
                            {
                                var variableIndex = (ushort)EsiUtilities.ParseHexDecString(x.Index.Value);
                                var subIndex = (byte)(byte.Parse(x.SubIndex) + indexOffset_Tmp);
                                //// Improve. What about -1 if SubIndex does not exist?
                                return new SlaveVariable(slavePdo, x.Name.FirstOrDefault()?.Value, variableIndex, subIndex, dataDirection, EcUtilities.GetOneDasDataTypeFromEthercatDataType(x.DataType?.Value), (byte)x.BitLen);
                            }).ToList();

                            slavePdo.SetVariables(slaveVariables);
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
            slaveInfo.DynamicData = new SlaveInfoDynamicData(name, description, pdos, base64ImageData);

            // execute extension logic
            ExtensibilityHelper.UpdateSlaveExtensions(extensionFactory, slaveInfo);

            slaveInfo.SlaveExtensions.ToList().ForEach(slaveExtension =>
            {
                slaveExtension.EvaluateSettings();
            });
        }

        public static void UpdateSlaveExtensions(IExtensionFactory extensionFactory, SlaveInfo slaveInfo, SlaveInfo referenceEcSlaveInfo = null)
        {
            TargetSlaveAttribute targetSlaveAttribute;

            var references = referenceEcSlaveInfo?.SlaveExtensions;

            if (references == null)
                references = slaveInfo.SlaveExtensions;

            var referenceTypes = references.Select(x => x.GetType()).ToList();

            var newSet = extensionFactory.Get<SlaveExtensionSettingsBase>().Where(type =>
            {
                if (!referenceTypes.Contains(type))
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

            slaveInfo.SlaveExtensions = references.Concat(newSet).ToList();

            // Improve: only required after deserialization, not after construction as the slaveInfo is passed a constructor parameter
            slaveInfo.SlaveExtensions.ToList().ForEach(slaveExtension => slaveExtension.SlaveInfo = slaveInfo);
        }

        public static SlaveInfo ReloadHardware(string esiDirectoryPath, IExtensionFactory extensionFactory, string interfaceName, SlaveInfo referenceRootSlaveInfo)
        {

            if (NetworkInterface.GetAllNetworkInterfaces().Where(x => x.GetPhysicalAddress().ToString() == interfaceName).FirstOrDefault()?.OperationalStatus != OperationalStatus.Up)
            {
                throw new Exception($"The network interface '{interfaceName}' is not linked. Aborting action.");
            }

            var context = EcHL.CreateContext();
            var newRootSlaveInfo = EcUtilities.ScanDevices(context, interfaceName, referenceRootSlaveInfo);
            EcHL.FreeContext(context);

            var referenceSlaveInfos = default(IEnumerable<SlaveInfo>);

            if (referenceRootSlaveInfo != null)
                referenceSlaveInfos = referenceRootSlaveInfo.Descendants().ToList();

            newRootSlaveInfo.Descendants().ToList().ForEach(slaveInfo =>
            {
                var referenceSlaveInfo = slaveInfo.Csa == slaveInfo.OldCsa 
                    ? referenceSlaveInfos?.FirstOrDefault(x => x.Csa == slaveInfo.Csa) 
                    : null;
                
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
