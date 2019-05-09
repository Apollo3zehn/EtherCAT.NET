namespace EtherCAT.NET.Extension
{
    public class DistributedClocksParameters
    {
        public DistributedClocksParameters(uint cycleTime0, uint cycleTime1, int shiftTime0)
        {
            this.CycleTime0 = cycleTime0;
            this.CycleTime1 = cycleTime1;
            this.ShiftTime0 = shiftTime0;
        }

        public uint CycleTime0 { get; private set; }
        public uint CycleTime1 { get; private set; }
        public int ShiftTime0 { get; private set; }
    }
}
