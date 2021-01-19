using System;
using System.Runtime.InteropServices;

namespace EtherCAT.NET.Infrastructure
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

        public ec_slave_info_t(SlaveInfo slave)
        {
            this.manufacturer = slave.Manufacturer;
            this.productCode = slave.ProductCode;
            this.revision = slave.Revision;
            this.oldCsa = slave.OldCsa;
            this.csa = slave.Csa;
            this.parentIndex = 0;
        }
    }
}