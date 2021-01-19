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
    [ExtensionContext(typeof(QBloxxEcA107Extension))]
    [ExtensionIdentification("QBloxxEcA107", "Q.Bloxx EC A107", "Q.bloxx-EC A107", @"WebClient.QBloxxEcA107View.html", @"WebClient.QBloxxEcA107.js")]
    [TargetSlave(0x0000050A, 0x0000BB81)]
    public class QBloxxEcA107Settings : SlaveExtensionSettingsBase
    {
        #region "Constructors"

        public QBloxxEcA107Settings(SlaveInfo slaveInfo) : base(slaveInfo)
        {
            this.Modules = new List<OneDasModule>();
        }

        #endregion

        #region "Properties"

        [DataMember]
        public List<OneDasModule> Modules { get; set; }

        #endregion

        #region "Methods"

        public override void EvaluateSettings()
        {
            var currentInputIndex = 0;
            var currentOutputIndex = 0;
            this.SlaveInfo.DynamicData.Pdos.Clear();

            // inputs
            this.Modules.ForEach(module =>
            {
                int currentIndex;
                ushort pdoAddress;
                ushort variableAddress;
                ushort syncManager;

                string prefix;

                switch (module.DataDirection)
                {
                    case DataDirection.Input:

                        pdoAddress = 0x1A00;
                        variableAddress = 0x6000;
                        syncManager = 0x03;
                        prefix = "RxPDO";
                        currentIndex = currentInputIndex;
                        currentInputIndex++;

                        break;

                    case DataDirection.Output:

                        pdoAddress = 0x1600;
                        variableAddress = 0x7000;
                        syncManager = 0x02;
                        prefix = "TxPDO";
                        currentIndex = currentOutputIndex;
                        currentOutputIndex++;

                        break;

                    default:
                        throw new ArgumentException();
                }

                var slavePdo = new SlavePdo(this.SlaveInfo, $"{ prefix } { currentIndex } ({ module.DataType })", (ushort)(pdoAddress + currentIndex), 0, true, true, syncManager);

                slavePdo.SetVariables(Enumerable.Range(0, module.Size).Select(currentVariableIndex =>
                {
                    return new SlaveVariable(slavePdo, $"Var { currentVariableIndex }", (ushort)(variableAddress + currentIndex * 10), 0x01, module.DataDirection, module.DataType);
                }).ToList());

                this.SlaveInfo.DynamicData.Pdos.Add(slavePdo);
            });
        }

        #endregion
    }
}
