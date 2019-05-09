using EtherCAT.NET.Extensibility;
using EtherCAT.NET.Infrastructure;
using OneDas.Extensibility;
using OneDas.Infrastructure;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.Serialization;

namespace EtherCAT.NET.Extension
{
    [DataContract]
    [ExtensionContext(typeof(EL6731_0010Extension))]
    [ExtensionIdentification("EL6731_0010", "EL6731-0010", "PROFIBUS slave terminal", @"WebClient.EL6731_0010View.html", @"WebClient.EL6731_0010.js")]
    [TargetSlave(0x00000002, 0x1a4b3052)]
    public class EL6731_0010Settings : SlaveExtensionSettingsBase
    {
        #region "Constructors"

        public EL6731_0010Settings(SlaveInfo slaveInfo) : base(slaveInfo)
        {
            this.SelectedModuleSet = new List<EL6731_0010Module>();
            this.StationNumber = 1;
        }

        #endregion

        #region "Properties"

        [DataMember]
        public byte StationNumber { get; set; }

        [DataMember]
        public List<EL6731_0010Module> SelectedModuleSet { get; set; }

        #endregion

        #region "Methods"

        public override void EvaluateSettings()
        {
            Contract.Requires(SelectedModuleSet != null);

            if (!(0 < this.StationNumber && this.StationNumber < 127))
            {
                throw new Exception(ExtensionErrorMessage.EL6731_0010Settings_StationNumberInvalid);
            }

            this.SlaveInfo.DynamicData.PdoSet.Clear();

            foreach (EL6731_0010Module el6731_0010_Module in this.SelectedModuleSet)
            {
                ushort pdoIndex = 0;
                ushort syncManager = 0;
                ushort variableIndex = 0;
                ushort arrayLength = 0;
                OneDasDataType dataType = default;
                DataDirection dataDirection = default;
                List<SlaveVariable> ecSlaveVariableSet = new List<SlaveVariable>();

                if ((int)el6731_0010_Module == 0x50) // word ouput
                {
                    dataType = OneDasDataType.UINT16;
                    dataDirection = DataDirection.Output;
                    arrayLength = (ushort)(el6731_0010_Module - 0x50 + 1);
                }
                else if (0xd1 <= (int)el6731_0010_Module && (int)el6731_0010_Module <= 0xdf)
                {
                    dataType = OneDasDataType.UINT16;
                    dataDirection = DataDirection.Output;
                    arrayLength = (ushort)(el6731_0010_Module - 0xd1 + 2);
                }
                else if (0x40d0 <= (int)el6731_0010_Module && (int)el6731_0010_Module <= 0x40ff)
                {
                    dataType = OneDasDataType.UINT16;
                    dataDirection = DataDirection.Output;
                    arrayLength = (ushort)(el6731_0010_Module - 0x40d0 + 16 + 1);
                }
                else if ((int)el6731_0010_Module == 0x60) // word input
                {
                    dataType = OneDasDataType.UINT16;
                    dataDirection = DataDirection.Input;
                    arrayLength = (ushort)(el6731_0010_Module - 0x60 + 1);
                }
                else if (0xe1 <= (int)el6731_0010_Module && (int)el6731_0010_Module <= 0xef)
                {
                    dataType = OneDasDataType.UINT16;
                    dataDirection = DataDirection.Input;
                    arrayLength = (ushort)(el6731_0010_Module - 0xe1 + 2);
                }
                else if (0x80d0 <= (int)el6731_0010_Module && (int)el6731_0010_Module <= 0x80fff)
                {
                    dataType = OneDasDataType.UINT16;
                    dataDirection = DataDirection.Input;
                    arrayLength = (ushort)(el6731_0010_Module - 0x80d0 + 16 + 1);
                }
                else if (0x10 <= (int)el6731_0010_Module && (int)el6731_0010_Module <= 0x1f) // byte output
                {
                    dataType = OneDasDataType.UINT8;
                    dataDirection = DataDirection.Output;
                    arrayLength = (ushort)(el6731_0010_Module - 0x10 + 1);
                }
                else if (0x20 <= (int)el6731_0010_Module && (int)el6731_0010_Module <= 0x2f) // byte input
                {
                    dataType = OneDasDataType.UINT8;
                    dataDirection = DataDirection.Input;
                    arrayLength = (ushort)(el6731_0010_Module - 0x20 + 1);
                }
                else
                {
                    throw new Exception(ExtensionErrorMessage.EL6731_0010Settings_ModuleInvalid);
                }

                // improve, implicit?
                switch (dataDirection)
                {
                    case DataDirection.Output:
                        pdoIndex = 0x1600;
                        syncManager = 2;
                        variableIndex = 0x7000;
                        break;
                    case DataDirection.Input:
                        pdoIndex = 0x1a00;
                        syncManager = 3;
                        variableIndex = 0x6000;
                        break;
                }

                SlavePdo slavePdo = new SlavePdo(this.SlaveInfo, el6731_0010_Module.ToString(), pdoIndex, 0, true, true, syncManager);
                ushort offset = (ushort)this.SlaveInfo.DynamicData.PdoSet.SelectMany(x => x.VariableSet).Where(x => x.DataDirection == dataDirection).Count();

                for (ushort i = (ushort)(offset + 0); i <= offset + arrayLength - 1; i++)
                {
                    ecSlaveVariableSet.Add(new SlaveVariable(slavePdo, i.ToString(), variableIndex, Convert.ToByte(i + 1), dataDirection, dataType));
                }

                slavePdo.SetVariableSet(ecSlaveVariableSet);
                this.SlaveInfo.DynamicData.PdoSet.Add(slavePdo);
            }

            // 1A80 improve! include Variables
            SlavePdo slavePdo2 = new SlavePdo(this.SlaveInfo, "Diagnostics", 0x1a80, 0, true, true, 3);
            slavePdo2.SetVariableSet(new List<SlaveVariable>());
            this.SlaveInfo.DynamicData.PdoSet.Add(slavePdo2);
        }

        #endregion
    }
}
