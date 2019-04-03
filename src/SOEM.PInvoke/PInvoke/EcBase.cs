using System;
using System.Runtime.InteropServices;
using System.Security;

namespace SOEM.PInvoke
{
    public class EcBase
    {
        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_setupdatagram(IntPtr port, IntPtr frame, byte com, byte idx, ushort ADP, ushort ADO, ushort length, IntPtr data);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_adddatagram(IntPtr port, IntPtr frame, byte com, byte idx, bool more, ushort ADP, ushort ADO, ushort length, IntPtr data);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_BWR(IntPtr port, ushort ADP, ushort ADO, ushort length, IntPtr data, int timeout);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_BRD(IntPtr port, ushort ADP, ushort ADO, ushort length, IntPtr data, int timeout);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_APRD(IntPtr port, ushort ADP, ushort ADO, ushort length, IntPtr data, int timeout);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_ARMW(IntPtr port, ushort ADP, ushort ADO, ushort length, IntPtr data, int timeout);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_FRMW(IntPtr port, ushort ADP, ushort ADO, ushort length, IntPtr data, int timeout);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern ushort ecx_APRDw(IntPtr port, ushort ADP, ushort ADO, int timeout);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_FPRD(IntPtr port, ushort ADP, ushort ADO, ushort length, IntPtr data, int timeout);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern ushort ecx_FPRDw(IntPtr port, ushort ADP, ushort ADO, int timeout);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_APWRw(IntPtr port, ushort ADP, ushort ADO, ushort data, int timeout);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_APWR(IntPtr port, ushort ADP, ushort ADO, ushort length, IntPtr data, int timeout);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_FPWRw(IntPtr port, ushort ADP, ushort ADO, ushort data, int timeout);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_FPWR(IntPtr port, ushort ADP, ushort ADO, ushort length, IntPtr data, int timeout);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_LRW(IntPtr port, uint LogAdr, ushort length, IntPtr data, int timeout);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_LRD(IntPtr port, uint LogAdr, ushort length, IntPtr data, int timeout);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_LWR(IntPtr port, uint LogAdr, ushort length, IntPtr data, int timeout);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_LRWDC(IntPtr port, uint LogAdr, ushort length, IntPtr data, ushort DCrs, out long DCtime, int timeout);
    }
}