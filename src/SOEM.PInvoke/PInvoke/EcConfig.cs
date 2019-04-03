using System;
using System.Runtime.InteropServices;
using System.Security;

namespace SOEM.PInvoke
{
    public class EcConfig
    {
        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_config_init(IntPtr context, byte usetable);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_config_map_group(IntPtr context, IntPtr pIOmap, byte group);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_config_overlap_map_group(IntPtr context, IntPtr pIOmap, byte group);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_recover_slave(IntPtr context, ushort slave, int timeout);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_reconfig_slave(IntPtr context, ushort slave, int timeout);
    }
}