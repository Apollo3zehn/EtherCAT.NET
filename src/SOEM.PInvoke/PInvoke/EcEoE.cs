using System;
using System.Runtime.InteropServices;
using System.Security;

namespace SOEM.PInvoke
{
    public class EcEoE
    {
        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_EOEdefinehook(IntPtr context, IntPtr hook);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_EOEsetIp(IntPtr context, ushort slave, byte port, IntPtr ipparam, int timeout);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_EOEgetIp(IntPtr context, ushort slave, byte port, IntPtr ipparam, int timeout);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_EOEsend(IntPtr context, ushort slave, byte port, int psize, IntPtr p, int timeout);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_EOErecv(IntPtr context, ushort slave, byte port, ref int psize, IntPtr p, int timeout);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_EOEreadfragment(IntPtr MbxIn, ref byte rxfragmentno, ref ushort rxframesize, ref ushort rxframeoffset, ref ushort rxframeno, ref int psize, IntPtr p);
    }
}