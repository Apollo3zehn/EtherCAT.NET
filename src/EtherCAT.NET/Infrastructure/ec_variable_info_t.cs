using System;
using System.Runtime.InteropServices;

namespace EtherCAT.NET.Infrastructure
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ec_variable_info_t
    {
        public UInt16 index;
        public byte subIndex;
        public string name;
        public EthercatDataType dataType;
    }
}