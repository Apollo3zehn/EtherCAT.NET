using EtherCAT.NET.Infrastructure;
using System;

namespace EtherCAT.NET.Extension
{
    public class DistributedClocksOpMode
    {
        public DistributedClocksOpMode(DeviceTypeDCOpMode opMode)
        {
            var assignActivate = EsiUtilities.ParseHexDecString(opMode.AssignActivate);

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

        public string Name { get; private set; }
        public string Description { get; private set; }

        // Cyclic mode
        public bool CycleTimeSyncUnit_IsEnabled { get; private set; }
        public int CycleTimeSyncUnit { get; private set; }

        // SYNC 0
        public bool CycleTimeSync0_IsEnabled { get; private set; }
        public int CycleTimeSync0 { get; private set; }
        public int CycleTimeSync0_Factor { get; private set; }
        public int ShiftTimeSync0 { get; private set; }
        public int ShiftTimeSync0_Factor { get; private set; }
        public bool ShiftTimeSync0_Input { get; private set; }

        // SYNC 1
        public bool CycleTimeSync1_IsEnabled { get; private set; }
        public int CycleTimeSync1 { get; private set; }
        public int CycleTimeSync1_Factor { get; private set; }
        public int ShiftTimeSync1 { get; private set; }
        public int ShiftTimeSync1_Factor { get; private set; }
        public bool ShiftTimeSync1_Input { get; private set; }
    }
}