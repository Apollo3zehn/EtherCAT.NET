using System;
using System.Runtime.InteropServices;
using System.Security;

namespace SOEM.PInvoke
{
    public class EcCoE
    {
        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern void ecx_SDOerror(IntPtr context, ushort Slave, ushort Index, byte SubIdx, int AbortCode);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_SDOread(IntPtr context, ushort slave, ushort index, byte subindex, bool CA, ref int psize, IntPtr p, int timeout);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_SDOwrite(IntPtr context, ushort Slave, ushort Index, byte SubIndex, bool CA, int psize, IntPtr p, int Timeout);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_RxPDO(IntPtr context, ushort Slave, ushort RxPDOnumber, int psize, IntPtr p);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_TxPDO(IntPtr context, ushort slave, ushort TxPDOnumber, ref int psize, IntPtr p, int timeout);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_readPDOmap(IntPtr context, ushort Slave, out int Osize, out int Isize);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_readPDOmapCA(IntPtr context, ushort Slave, int Thread_n, out int Osize, out int Isize);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_readODlist(IntPtr context, ushort Slave, IntPtr pODlist);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_readODdescription(IntPtr context, ushort Item, IntPtr pODlist);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_readOEsingle(IntPtr context, ushort Item, byte SubI, IntPtr pODlist, IntPtr pOElist);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_readOE(IntPtr context, ushort Item, IntPtr pODlist, IntPtr pOElist);
    }
}