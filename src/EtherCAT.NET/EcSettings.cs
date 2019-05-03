namespace EtherCAT
{
    public class EcSettings
    {
        #region Constructors

        public EcSettings(uint cycleFrequency, string esiDirectoryPath, string nicHardwareAddress)
        {
            this.CycleFrequency = cycleFrequency;
            this.FrameCount = 15000;
            this.TargetTimeDifference = 100;
            this.DriftCompensationRate = 850000;

            this.IoMapLength = 4096;
            this.MaxRetries = 3;
            this.WatchdogSleepTime = 1;

            this.NicHardwareAddress = nicHardwareAddress;
            this.EsiDirectoryPath = esiDirectoryPath;
        }

        #endregion

        #region Properties

        public uint CycleFrequency { get; }
        public uint FrameCount { get; set; }
        public uint TargetTimeDifference { get; set; }
        public uint DriftCompensationRate { get; set; }

        public int IoMapLength { get; set; }
        public int MaxRetries { get; set; }
        public int WatchdogSleepTime { get; set; }

        public string NicHardwareAddress { get; set; }
        public string EsiDirectoryPath { get; }

        #endregion

        #region Methods

        public EcSettings ShallowCopy()
        {
            return (EcSettings)this.MemberwiseClone();
        }

        #endregion
    }
}