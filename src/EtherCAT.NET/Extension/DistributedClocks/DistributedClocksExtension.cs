using EtherCAT.NET.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EtherCAT.NET.Extension
{
    public class DistributedClocksExtension : SlaveExtension
    {
        #region "Constructors"

        public DistributedClocksExtension(SlaveInfo slaveInfo) : base(slaveInfo)
        {
            this.SelectedOpModeId = slaveInfo.Esi.Dc.OpMode.First().Name;
        }

        #endregion

        #region "Properties"

        public string SelectedOpModeId { get; set; }

        #endregion

        #region "Methods"

        public override void EvaluateSettings()
        {
            var dcOpMode = this.Slave.Esi.Dc.OpMode.Where(x => x.Name == SelectedOpModeId).First();
            var syncManagerPdosSet = dcOpMode.Sm ?? new DeviceTypeDCOpModeSM[] { };

            if (syncManagerPdosSet.Count() > 0)
            {
                this.Slave.DynamicData.Pdos.ToList().ForEach(x => x.SyncManager = -1);

                foreach (var syncManagerPdos in syncManagerPdosSet)
                {
                    int syncManager = syncManagerPdos.No;

                    foreach (var smPdo in syncManagerPdos.Pdo)
                    {
                        var index = (ushort)EsiUtilities.ParseHexDecString(smPdo.Value);
                        var currentOsFactor = (ushort)(smPdo.OSFacSpecified ? smPdo.OSFac : 1);

                        for (ushort osFactorIndex = 1; osFactorIndex <= currentOsFactor; osFactorIndex++)
                        {
                            var indexOffset = (ushort)(osFactorIndex - 1);
                            this.Slave.DynamicData.Pdos.Where(x => x.Index == index + indexOffset).First().SyncManager = syncManager;
                        }
                    }
                }
            }
        }

        public override IEnumerable<SdoWriteRequest> GetSdoWriteRequests()
        {
            return new List<SdoWriteRequest> { };
        }

        public DistributedClocksParameters CalculateDcParameters(ref byte[] assignActivate, uint cycleFrequency)
        {
            var cycleTime0 = 0U;
            var cycleTime1 = 0U;
            var shiftTime0 = 0;
            var dcOpMode = this.Slave.Esi.Dc.OpMode.Where(x => x.Name == SelectedOpModeId).First();

            assignActivate = null;

            if (dcOpMode != null)
            {
                var assignActivate_Tmp = (int)EsiUtilities.ParseHexDecString(dcOpMode.AssignActivate);

                if (assignActivate_Tmp == 0)
                {
                    assignActivate = new byte[] { 0 };
                }
                else
                {
                    assignActivate = new byte[]
                    {
                        Convert.ToByte(assignActivate_Tmp & 0xff),
                        Convert.ToByte((assignActivate_Tmp & 0xff00) >> 8)
                    };
                }

                var cycleTimeSyncUnit = Convert.ToInt32(Math.Pow(10, 9) / cycleFrequency);

                cycleTime0 = Convert.ToUInt32(dcOpMode.CycleTimeSync0?.Value);

                if (dcOpMode.CycleTimeSync0 != null && cycleTime0 == 0)
                {
                    if (dcOpMode.CycleTimeSync0.Factor >= 0)
                        cycleTime0 = Convert.ToUInt32(cycleTimeSyncUnit * Math.Abs(dcOpMode.CycleTimeSync0.Factor));
                    else
                        cycleTime0 = Convert.ToUInt32(cycleTimeSyncUnit / Math.Abs(dcOpMode.CycleTimeSync0.Factor));
                 }

                // shiftTime0
                shiftTime0 = Convert.ToInt32(dcOpMode.ShiftTimeSync0?.Value);

                if (dcOpMode.ShiftTimeSync0 != null && dcOpMode.ShiftTimeSync0.FactorSpecified)
                {
                    if (dcOpMode.ShiftTimeSync0.Factor >= 0)
                        shiftTime0 += Convert.ToInt32(cycleTime0 * Math.Abs(dcOpMode.ShiftTimeSync0.Factor));
                    else
                        shiftTime0 += Convert.ToInt32(cycleTime0 / Math.Abs(dcOpMode.ShiftTimeSync0.Factor));
                }

                // shiftTime1
                int shiftTime1 = Convert.ToInt32(dcOpMode.ShiftTimeSync1?.Value);

                if (dcOpMode.ShiftTimeSync1 != null && dcOpMode.ShiftTimeSync1.FactorSpecified)
                {
                    // for future use
                }

                // cycleTime1
                cycleTime1 = Convert.ToUInt32(dcOpMode.CycleTimeSync1?.Value);

                if (dcOpMode.CycleTimeSync1 != null && cycleTime1 == 0)
                {
                    if (dcOpMode.CycleTimeSync1.Factor >= 0)
                        cycleTime1 = Convert.ToUInt32(cycleTime0 * Math.Abs(dcOpMode.CycleTimeSync1.Factor));
                    else
                        cycleTime1 = Convert.ToUInt32(cycleTimeSyncUnit / Math.Abs(dcOpMode.CycleTimeSync1.Factor));

                    cycleTime1 = Convert.ToUInt32(cycleTime1 - cycleTime0 + shiftTime1);
                }
            }

            return new DistributedClocksParameters(cycleTime0, cycleTime1, shiftTime0);
        }

        #endregion
    }
}
