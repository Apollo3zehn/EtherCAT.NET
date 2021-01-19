using System;
using System.Runtime.InteropServices;

namespace EtherCAT.NET.Infrastructure
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ec_pdo_info_t
    {
        public UInt16 index;
        public string name;
        public int variableCount;
        public IntPtr variableInfos;
    }
}