using System;
using System.Runtime.InteropServices;
using System.Security;

namespace SOEM.PInvoke
{
    public class EcSoE
    {
        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_SoEread(IntPtr context, ushort slave, byte driveNo, byte elementflags, ushort idn, ref int psize, IntPtr p, int timeout);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_SoEwrite(IntPtr context, ushort slave, byte driveNo, byte elementflags, ushort idn, int psize, IntPtr p, int timeout);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_readIDNmap(IntPtr context, ushort slave, out int Osize, out int Isize);
    }
}