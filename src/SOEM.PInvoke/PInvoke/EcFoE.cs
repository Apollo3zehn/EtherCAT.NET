using System;
using System.Runtime.InteropServices;
using System.Security;

namespace SOEM.PInvoke
{
    public class EcFoE
    {
        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_FOEdefinehook(IntPtr context, IntPtr hook);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_FOEread(IntPtr context, ushort slave, [MarshalAs(UnmanagedType.LPStr)]string filename, uint password, ref int psize, IntPtr p, int timeout);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_FOEwrite(IntPtr context, ushort slave, [MarshalAs(UnmanagedType.LPStr)]string filename, uint password, int psize, IntPtr p, int timeout);
    }
}