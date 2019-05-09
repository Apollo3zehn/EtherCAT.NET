using EtherCAT.NET.Extensibility;
using EtherCAT.NET.Infrastructure;
using OneDas.Extensibility;
using OneDas.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace EtherCAT.NET.Extension
{
    [DataContract]
    [ExtensionContext(typeof(AnybusXGatewayExtension))]
    [ExtensionIdentification("AnybusXGateway", "Anybus X-Gateway", "Modbus RTU - EtherCAT Slave Gateway", @"WebClient.AnybusXGatewayView.html", @"WebClient.AnybusXGateway.js")]
    [TargetSlave(0x0000001B, 0x0000003D)]
    public class AnybusXGatewaySettings : SlaveExtensionSettingsBase
    {
        #region "Constructors"

        public AnybusXGatewaySettings(SlaveInfo slaveInfo) : base(slaveInfo)
        {
            this.ModuleSet = new List<OneDasModule>();
        }

        #endregion

        #region "Properties"

        [DataMember]
        public List<OneDasModule> ModuleSet { get; set; }

        #endregion

        #region "Methods"

        public override void EvaluateSettings()
        {
            int consumedBytes;
            int currentPdo;
            int currentIndex;
            int currentBytes;

            SlavePdo currentSlavePdo;

            //
            this.SlaveInfo.DynamicData.PdoSet.Clear();

            // inputs
            consumedBytes = 0;
            currentPdo = 0;
            currentIndex = 0;

            this.ModuleSet.Where(module => module.DataDirection == DataDirection.Input).ToList().ForEach(module =>
            {
                if (currentPdo > 4)
                {
                    throw new Exception(ExtensionErrorMessage.AnybusXGatewaySettings_TooManyModules);
                }

                if (module.DataType == OneDasDataType.BOOLEAN)
                {
                    throw new Exception(ExtensionErrorMessage.AnybusXGatewaySettings_InvalidDatatype);
                }

                currentBytes = ((int)module.DataType & 0x0FF) / 8 * module.Size;

                if (consumedBytes + currentBytes > 0x80)
                {
                    throw new Exception(ExtensionErrorMessage.AnybusXGatewaySettings_InvalidSize);
                }

                currentSlavePdo = new SlavePdo(this.SlaveInfo, $"RxPDO { currentIndex } ({ module.Size }x { module.DataType })", (ushort)(0x1A00 + currentPdo), 0, true, true, 0x03);

                currentSlavePdo.SetVariableSet(Enumerable.Range(0, module.Size).Select(currentVariableIndex =>
                {
                    return new SlaveVariable(currentSlavePdo, $"Var { currentVariableIndex }", (ushort)(0x2000 + currentPdo), (byte)(consumedBytes + currentVariableIndex), DataDirection.Input, module.DataType);
                }).ToList());

                this.SlaveInfo.DynamicData.PdoSet.Add(currentSlavePdo);

                currentIndex++;
                consumedBytes += currentBytes;

                if (consumedBytes == 0x80)
                {
                    consumedBytes = 0;
                    currentBytes = 0;
                    currentPdo++;
                }
            });

            // outputs
            consumedBytes = 0;
            currentPdo = 0;
            currentIndex = 0;

            this.ModuleSet.Where(module => module.DataDirection == DataDirection.Output).ToList().ForEach(module =>
            {
                if (currentPdo > 4)
                {
                    throw new Exception(ExtensionErrorMessage.AnybusXGatewaySettings_TooManyModules);
                }

                if (module.DataType == OneDasDataType.BOOLEAN)
                {
                    throw new Exception(ExtensionErrorMessage.AnybusXGatewaySettings_InvalidDatatype);
                }

                currentBytes = ((int)module.DataType & 0x0FF) / 8 * module.Size;

                if (consumedBytes + currentBytes > 0x80)
                {
                    throw new Exception(ExtensionErrorMessage.AnybusXGatewaySettings_InvalidSize);
                }

                currentSlavePdo = new SlavePdo(this.SlaveInfo, $"TxPDO { currentIndex } ({ module.Size }x { module.DataType })", (ushort)(0x1600 + currentPdo), 0, true, true, 0x02);

                currentSlavePdo.SetVariableSet(Enumerable.Range(0, module.Size).Select(currentVariableIndex =>
                {
                    return new SlaveVariable(currentSlavePdo, $"Var { currentVariableIndex }", (ushort)(0x2000 + currentPdo), (byte)(consumedBytes + currentVariableIndex), DataDirection.Output, module.DataType);
                }).ToList());

                this.SlaveInfo.DynamicData.PdoSet.Add(currentSlavePdo);

                currentIndex++;
                consumedBytes += currentBytes;

                if (consumedBytes == 0x80)
                {
                    consumedBytes = 0;
                    currentBytes = 0;
                    currentPdo++;
                }
            });
        }

        #endregion
    }
}
