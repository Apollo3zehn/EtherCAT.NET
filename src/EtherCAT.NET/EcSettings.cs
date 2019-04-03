using EtherCAT.Infrastructure;

namespace EtherCAT
{
    public class EcSettings
    {
        #region Constructors

        public EcSettings(string esiDirectoryPath, uint nativeSampleRate)
        {
            this.RootSlaveInfo = null;

            this.NativeSampleRate = nativeSampleRate;
            this.FrameCount = 15000;
            this.TargetTimeDifference = 100;
            this.DriftCompensationRate = 850000;

            this.IoMapLength = 4096;
            this.MaxRetries = 3;
            this.WatchdogSleepTime = 1;

            this.NicHardwareAddress = string.Empty;
            this.EsiDirectoryPath = esiDirectoryPath;
        }

        #endregion

        #region Properties

        public SlaveInfo RootSlaveInfo { get; set; }

        public uint NativeSampleRate { get; }
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