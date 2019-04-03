using System;
using System.Runtime.InteropServices;
using System.Security;

namespace SOEM.PInvoke
{
    public class EcMain
    {
        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern IntPtr ec_find_adapters();

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern void ec_free_adapters(IntPtr adapter);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern byte ec_nextmbxcnt(byte cnt);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern void ec_clearmbx(IntPtr Mbx);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern void ecx_pusherror(IntPtr context, IntPtr Ec);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern bool ecx_poperror(IntPtr context, IntPtr Ec);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern bool ecx_iserror(IntPtr context);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern void ecx_packeterror(IntPtr context, ushort Slave, ushort Index, byte SubIdx, ushort ErrorCode);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_init(IntPtr context, [MarshalAs(UnmanagedType.LPStr)]string ifname);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_init_redundant(IntPtr context, IntPtr redport, [MarshalAs(UnmanagedType.LPStr)]string ifname, [MarshalAs(UnmanagedType.LPStr)]string if2name);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern void ecx_close(IntPtr context);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern byte ecx_siigetbyte(IntPtr context, ushort slave, ushort address);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern short ecx_siifind(IntPtr context, ushort slave, ushort cat);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern void ecx_siistring(IntPtr context, [MarshalAs(UnmanagedType.LPStr)]string str, ushort slave, ushort Sn);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern ushort ecx_siiFMMU(IntPtr context, ushort slave, IntPtr FMMU);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern ushort ecx_siiSM(IntPtr context, ushort slave, IntPtr SM);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern ushort ecx_siiSMnext(IntPtr context, ushort slave, IntPtr SM, ushort n);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_siiPDO(IntPtr context, ushort slave, IntPtr PDO, byte t);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_readstate(IntPtr context);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_writestate(IntPtr context, ushort slave);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern ushort ecx_statecheck(IntPtr context, ushort slave, ushort reqstate, int timeout);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_mbxempty(IntPtr context, ushort slave, int timeout);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_mbxsend(IntPtr context, ushort slave, IntPtr mbx, int timeout);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_mbxreceive(IntPtr context, ushort slave, IntPtr mbx, int timeout);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern void ecx_esidump(IntPtr context, ushort slave, out byte esibuf);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern uint ecx_readeeprom(IntPtr context, ushort slave, ushort eeproma, int timeout);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_writeeeprom(IntPtr context, ushort slave, ushort eeproma, ushort data, int timeout);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_eeprom2master(IntPtr context, ushort slave);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_eeprom2pdi(IntPtr context, ushort slave);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern ulong ecx_readeepromAP(IntPtr context, ushort aiadr, ushort eeproma, int timeout);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_writeeepromAP(IntPtr context, ushort aiadr, ushort eeproma, ushort data, int timeout);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern ulong ecx_readeepromFP(IntPtr context, ushort configadr, ushort eeproma, int timeout);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_writeeepromFP(IntPtr context, ushort configadr, ushort eeproma, ushort data, int timeout);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern void ecx_readeeprom1(IntPtr context, ushort slave, ushort eeproma);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern uint ecx_readeeprom2(IntPtr context, ushort slave, int timeout);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_send_overlap_processdata_group(IntPtr context, byte group);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_receive_processdata_group(IntPtr context, byte group, int timeout);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_send_processdata(IntPtr context);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_send_overlap_processdata(IntPtr context);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_receive_processdata(IntPtr context, int timeout);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ecx_send_processdata_group(IntPtr context, byte group);
    }
}