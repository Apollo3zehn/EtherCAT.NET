using EtherCAT.NET.Extensibility;
using EtherCAT.NET.Infrastructure;
using OneDas.Extensibility;
using OneDas.Infrastructure;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace EtherCAT.NET.Extension
{
    [DataContract]
    [ExtensionContext(typeof(EL6751_0010Extension))]
    [ExtensionIdentification("EL6751_0010", "EL6751-0010", "CANopen slave terminal", @"WebClient.EL6751_0010View.html", @"WebClient.EL6751_0010.js")]
    [TargetSlave(0x00000002, 0x1a5f3052)]
    public class EL6751_0010Settings : SlaveExtensionSettingsBase
    {
        #region "Constructors"

        public EL6751_0010Settings(SlaveInfo slaveInfo) : base(slaveInfo)
        {
            this.SelectedModuleSet = new List<EL6751_0010Module>();
        }

        #endregion

        #region "Properties"

        [DataMember]
        public byte StationNumber { get; set; }

        [DataMember]
        public List<EL6751_0010Module> SelectedModuleSet { get; set; }

        #endregion

        #region "Methods"

        public override void EvaluateSettings()
        {
            // dont forget validation!






            //this.SlaveInfo.DynamicData.PdoSet.Clear();

            //foreach (EL6751_0010Module el6751_0010_Module in this.SelectedModuleSet)
            //{
            //    ushort pdoIndex = 0;
            //    ushort syncManager = 0;
            //    ushort variableIndex = 0;
            //    ushort arrayLength = 0;
            //    OneDasDataType dataType = default;
            //    DataDirection dataDirection = default;
            //    List<SlaveVariable> ecSlaveVariableSet = new List<SlaveVariable>();

            //    if ((int)el6751_0010_Module == 0x50) // word ouput
            //    {
            //        dataType = OneDasDataType.UINT16;
            //        dataDirection = DataDirection.Output;
            //        arrayLength = (ushort)(el6751_0010_Module - 0x50 + 1);
            //    }
            //    else if (0xd1 <= (int)el6751_0010_Module && (int)el6751_0010_Module <= 0xdf)
            //    {
            //        dataType = OneDasDataType.UINT16;
            //        dataDirection = DataDirection.Output;
            //        arrayLength = (ushort)(el6751_0010_Module - 0xd1 + 2);
            //    }
            //    else if (0x40d0 <= (int)el6751_0010_Module && (int)el6751_0010_Module <= 0x40ff)
            //    {
            //        dataType = OneDasDataType.UINT16;
            //        dataDirection = DataDirection.Output;
            //        arrayLength = (ushort)(el6751_0010_Module - 0x40d0 + 16 + 1);
            //    }
            //    else if ((int)el6751_0010_Module == 0x60) // word input
            //    {
            //        dataType = OneDasDataType.UINT16;
            //        dataDirection = DataDirection.Input;
            //        arrayLength = (ushort)(el6751_0010_Module - 0x60 + 1);
            //    }
            //    else if (0xe1 <= (int)el6751_0010_Module && (int)el6751_0010_Module <= 0xef)
            //    {
            //        dataType = OneDasDataType.UINT16;
            //        dataDirection = DataDirection.Input;
            //        arrayLength = (ushort)(el6751_0010_Module - 0xe1 + 2);
            //    }
            //    else if (0x80d0 <= (int)el6751_0010_Module && (int)el6751_0010_Module <= 0x80fff)
            //    {
            //        dataType = OneDasDataType.UINT16;
            //        dataDirection = DataDirection.Input;
            //        arrayLength = (ushort)(el6751_0010_Module - 0x80d0 + 16 + 1);
            //    }
            //    else if (0x10 <= (int)el6751_0010_Module && (int)el6751_0010_Module <= 0x1f) // byte output
            //    {
            //        dataType = OneDasDataType.UINT8;
            //        dataDirection = DataDirection.Output;
            //        arrayLength = (ushort)(el6751_0010_Module - 0x10 + 1);
            //    }
            //    else if (0x20 <= (int)el6751_0010_Module && (int)el6751_0010_Module <= 0x2f) // byte input
            //    {
            //        dataType = OneDasDataType.UINT8;
            //        dataDirection = DataDirection.Input;
            //        arrayLength = (ushort)(el6751_0010_Module - 0x20 + 1);
            //    }
            //    else
            //    {
            //        throw new Exception("Invalid module.");
            //    }

            //    // improve, implicit?
            //    switch (dataDirection)
            //    {
            //        case DataDirection.Output:
            //            pdoIndex = 0x1600;
            //            syncManager = 2;
            //            variableIndex = 0x7000;
            //            break;
            //        case DataDirection.Input:
            //            pdoIndex = 0x1a00;
            //            syncManager = 3;
            //            variableIndex = 0x6000;
            //            break;
            //    }

            //    SlavePdo slavePdo = new SlavePdo(this.SlaveInfo, el6751_0010_Module.ToString(), pdoIndex, 0, true, true, syncManager);
            //    ushort offset = (ushort)this.SlaveInfo.DynamicData.PdoSet.SelectMany(x => x.VariableSet).Where(x => x.DataDirection == dataDirection).Count();

            //    for (ushort i = (ushort)(offset + 0); i <= offset + arrayLength - 1; i++)
            //    {
            //        ecSlaveVariableSet.Add(new SlaveVariable(slavePdo, i.ToString(), variableIndex, Convert.ToByte(i + 1), InfrastructureHelper.GetBitLength(dataType, false), dataDirection, dataType));
            //    }

            //    slavePdo.SetVariableSet(ecSlaveVariableSet);
            //    this.SlaveInfo.DynamicData.PdoSet.Add(slavePdo);
            //}

            //// 1A80 improve! include Variables
            //SlavePdo slavePdo2 = new SlavePdo(this.SlaveInfo, "Diagnostics", 0x1a80, 0, true, true, 3);
            //slavePdo2.SetVariableSet(new List<SlaveVariable>());
            //this.SlaveInfo.DynamicData.PdoSet.Add(slavePdo2);








            SlavePdo slavePdo;

            this.SlaveInfo.DynamicData.PdoSet.Clear();

            // EL6751-0010 (CANopen Slave)_IN
            slavePdo = new SlavePdo(this.SlaveInfo, "EL6751-0010 (CANopen Slave)_IN", 0x1A00, 0, true, true, 0x03);

            slavePdo.SetVariableSet(new List<SlaveVariable>
            {
                new SlaveVariable(slavePdo, "CAN RxPDO 1", 0x6000, 0x01, DataDirection.Input, OneDasDataType.INT8)
            });

            this.SlaveInfo.DynamicData.PdoSet.Add(slavePdo);

            // CANopen Status
            slavePdo = new SlavePdo(this.SlaveInfo, "CANopen Status", 0x1A83, 0, true, true, 0x03);

            slavePdo.SetVariableSet(new List<SlaveVariable>
            {
                new SlaveVariable(slavePdo, "CANopenState", 0xF100, 0x01, DataDirection.Input, OneDasDataType.UINT16),
                new SlaveVariable(slavePdo, "TxPDO State", 0x1800, 0x07, DataDirection.Input, OneDasDataType.BOOLEAN)
            });

            this.SlaveInfo.DynamicData.PdoSet.Add(slavePdo);

            // CAN Diagnosis
            slavePdo = new SlavePdo(this.SlaveInfo, "CAN Diagnosis", 0x1A84, 0, true, true, 0x03);

            slavePdo.SetVariableSet(new List<SlaveVariable>
            {
                new SlaveVariable(slavePdo, "Device Diag", 0xF101, 0x0D, DataDirection.Input, OneDasDataType.BOOLEAN),
                new SlaveVariable(slavePdo, "Sync Error", 0xF101, 0x0E, DataDirection.Input, OneDasDataType.BOOLEAN),
                new SlaveVariable(slavePdo, "Device Toggle", 0xF101, 0x0F, DataDirection.Input, OneDasDataType.BOOLEAN),
                new SlaveVariable(slavePdo, "Device State", 0xF101, 0x10, DataDirection.Input, OneDasDataType.BOOLEAN),
                new SlaveVariable(slavePdo, "Cycle Counter", 0xF101, 0x11, DataDirection.Input, OneDasDataType.UINT16),
                new SlaveVariable(slavePdo, "Slave Status Counter", 0xF101, 0x12, DataDirection.Input, OneDasDataType.UINT8),
                new SlaveVariable(slavePdo, "Actual Cycle Time", 0xF101, 0x14, DataDirection.Input, OneDasDataType.UINT16),

                new SlaveVariable(slavePdo, "Rx error counter", 0xF108, 0x21, DataDirection.Input, OneDasDataType.UINT8),
                new SlaveVariable(slavePdo, "Tx error counter", 0xF108, 0x22, DataDirection.Input, OneDasDataType.UINT8),
                new SlaveVariable(slavePdo, "Bus off", 0xF108, 0x01, DataDirection.Input, OneDasDataType.BOOLEAN),
                new SlaveVariable(slavePdo, "Warning limit reached", 0xF108, 0x02, DataDirection.Input, OneDasDataType.BOOLEAN),
                new SlaveVariable(slavePdo, "Rx overflow", 0xF108, 0x03, DataDirection.Input, OneDasDataType.BOOLEAN),
                new SlaveVariable(slavePdo, "Tx overflow", 0xF108, 0x04, DataDirection.Input, OneDasDataType.BOOLEAN),
                new SlaveVariable(slavePdo, "Ack error", 0xF108, 0x05, DataDirection.Input, OneDasDataType.BOOLEAN),
            });

            this.SlaveInfo.DynamicData.PdoSet.Add(slavePdo);
        }

        #endregion
    }
}
