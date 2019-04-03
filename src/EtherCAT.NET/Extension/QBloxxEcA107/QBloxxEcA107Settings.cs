using EtherCAT.Extensibility;
using EtherCAT.Infrastructure;
using OneDas.Extensibility;
using OneDas.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace EtherCAT.Extension
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
            SlavePdo slavePdo;
            int currentInputIndex;
            int currentOutputIndex;

            //
            currentInputIndex = 0;
            currentOutputIndex = 0;
            this.SlaveInfo.DynamicData.PdoSet.Clear();

            // inputs
            this.ModuleSet.ForEach(module =>
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

                slavePdo = new SlavePdo(this.SlaveInfo, $"{ prefix } { currentIndex } ({ module.DataType })", (ushort)(pdoAddress + currentIndex), 0, true, true, syncManager);

                slavePdo.SetVariableSet(Enumerable.Range(0, module.Size).Select(currentVariableIndex =>
                {
                    return new SlaveVariable(slavePdo, $"Var { currentVariableIndex }", (ushort)(variableAddress + currentIndex * 10), 0x01, module.DataDirection, module.DataType);
                }).ToList());

                this.SlaveInfo.DynamicData.PdoSet.Add(slavePdo);
            });
        }

        #endregion
    }
}
