using System;
using System.Runtime.InteropServices;
using System.Security;

namespace SOEM.PInvoke
{
    public class EcDc
    {
        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern bool ecx_configdc(IntPtr context);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern void ecx_dcsync0(IntPtr context, ushort slave, bool act, uint CyclTime, int CyclShift);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern void ecx_dcsync01(IntPtr context, ushort slave, bool act, uint CyclTime0, uint CyclTime1, int CyclShift);
    }
}