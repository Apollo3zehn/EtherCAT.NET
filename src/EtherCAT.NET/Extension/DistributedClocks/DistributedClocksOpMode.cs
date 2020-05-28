using EtherCAT.NET.Infrastructure;
using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace EtherCAT.NET.Extension
{
    [DataContract]
    public class DistributedClocksOpMode
    {
        public DistributedClocksOpMode(DeviceTypeDCOpMode opMode)
        {
            var assignActivate = EsiUtilities.ParseHexDecString(opMode.AssignActivate);

            assignActivate = Int32.Parse(opMode.AssignActivate.Substring(2), NumberStyles.HexNumber);

            this.Name = opMode.Name;
            this.Description = opMode.Desc;

            // Cyclic mode
            this.CycleTimeSyncUnit_IsEnabled = (assignActivate & 0x100) > 0;
            this.CycleTimeSyncUnit = (int)(Math.Pow(10, 9) / 100); // improve! remove magic number

            // SYNC 0
            this.CycleTimeSync0_IsEnabled = (assignActivate & 0x200) > 0;
            this.CycleTimeSync0 = Convert.ToInt32(opMode.CycleTimeSync0?.Value);
            this.CycleTimeSync0_Factor = Convert.ToInt32(opMode.CycleTimeSync0?.Factor);
            this.ShiftTimeSync0 = Convert.ToInt32(opMode.ShiftTimeSync0?.Value);
            this.ShiftTimeSync0_Factor = Convert.ToInt32(opMode.ShiftTimeSync0?.Factor);
            this.ShiftTimeSync0_Input = Convert.ToBoolean(opMode.ShiftTimeSync0?.Input);

            // SYNC 1
            this.CycleTimeSync1_IsEnabled = (assignActivate & 0x400) > 0;
            this.CycleTimeSync1 = Convert.ToInt32(opMode.CycleTimeSync1?.Value);
            this.CycleTimeSync1_Factor = Convert.ToInt32(opMode.CycleTimeSync1?.Factor);
            this.ShiftTimeSync1 = Convert.ToInt32(opMode.ShiftTimeSync1?.Value);
            this.ShiftTimeSync1_Factor = Convert.ToInt32(opMode.ShiftTimeSync1?.Factor);
            this.ShiftTimeSync1_Input = Convert.ToBoolean(opMode.ShiftTimeSync1?.Input);
        }

        [DataMember]
        public string Name { get; private set; }
        [DataMember]
        public string Description { get; private set; }

        // Cyclic mode
        [DataMember]
        public bool CycleTimeSyncUnit_IsEnabled { get; private set; }
        [DataMember]
        public int CycleTimeSyncUnit { get; private set; }

        // SYNC 0
        [DataMember]
        public bool CycleTimeSync0_IsEnabled { get; private set; }
        [DataMember]
        public int CycleTimeSync0 { get; private set; }
        [DataMember]
        public int CycleTimeSync0_Factor { get; private set; }
        [DataMember]
        public int ShiftTimeSync0 { get; private set; }
        [DataMember]
        public int ShiftTimeSync0_Factor { get; private set; }
        [DataMember]
        public bool ShiftTimeSync0_Input { get; private set; }

        // SYNC 1
        [DataMember]
        public bool CycleTimeSync1_IsEnabled { get; private set; }
        [DataMember]
        public int CycleTimeSync1 { get; private set; }
        [DataMember]
        public int CycleTimeSync1_Factor { get; private set; }
        [DataMember]
        public int ShiftTimeSync1 { get; private set; }
        [DataMember]
        public int ShiftTimeSync1_Factor { get; private set; }
        [DataMember]
        public bool ShiftTimeSync1_Input { get; private set; }
    }
}