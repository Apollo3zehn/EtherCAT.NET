using System;
using System.Runtime.InteropServices;

namespace EtherCAT.Infrastructure
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ec_slave_info_t
    {
        public UInt32 manufacturer;
        public UInt32 productCode;
        public UInt32 revision;
        public UInt16 oldCsa;
        public UInt16 csa;
        public UInt16 parentIndex;

        public ec_slave_info_t(SlaveInfo slaveInfo)
        {
            this.manufacturer = slaveInfo.Manufacturer;
            this.productCode = slaveInfo.ProductCode;
            this.revision = slaveInfo.Revision;
            this.oldCsa = slaveInfo.OldCsa;
            this.csa = slaveInfo.Csa;
            this.parentIndex = 0;
        }
    }
}