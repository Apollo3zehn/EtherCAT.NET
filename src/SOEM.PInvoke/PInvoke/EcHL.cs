using System;
using System.Runtime.InteropServices;
using System.Security;

namespace SOEM.PInvoke
{
    public static class EcHL
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double PO2SOCallback(UInt16 slaveIndex);

        #region "Helper"

        #endregion

        #region "P/Invoke low level"

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern IntPtr CreateContext();

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern void FreeContext(IntPtr context);

        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern void Free(IntPtr obj);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool HasEcError(IntPtr context);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern IntPtr GetNextError(IntPtr context);

        #endregion

        #region "called before OP"

        /// <summary>
        /// Initializes EtherCAT and scans for connected slaves.
        /// </summary>
        /// <param name="networkInterfaceName">The name of the network interface which is connected to the EtherCAT network.</param>
        /// <param name="slaveIdentifications">A list of <see cref="IntPtr"/> where each one points to a <see cref="ec_slave_info_t"/>.</param>
        /// <param name="slaveCount">The number of slaves found.</param>
        /// <returns>Returns the status code of the unmanaged operation.</returns>
        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ScanDevices(IntPtr context, string networkInterfaceName, out IntPtr slaveIdentifications, out int slaveCount);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int UpdateCsa(IntPtr context, int slaveIndex, int csaBase);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int UploadPdoConfig(IntPtr context, UInt16 slave, UInt16 smIndex, out IntPtr pdoInfos, out int pdoCount);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int GetSyncManagerType(IntPtr context, UInt16 slave, int index, out SyncManagerType syncManagerType);

        /// <summary>
        /// Write service data object data to the mailbox of the corresponding slave.
        /// </summary>
        /// <param name="slaveIndex">The index of the corresponding slave.</param>
        /// <param name="sdoIndex">The index of the service data object.</param>
        /// <param name="data">The data to write.</param>
        /// <param name="byteLength">The lengths of the data to write.</param>
        /// <returns>Returns the status code of the unmanaged operation.</returns>
        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int SdoWrite(IntPtr context, UInt16 slaveIndex, UInt16 sdoIndex, byte sdoSubIndex, byte[] dataset, UInt32 datasetCount, Int32[] byteCounts);

        /// <summary>
        /// Configures the sync managers, FMMUs and the IO map of the EtherCAT network.
        /// </summary>
        /// <param name="ioMapPtr">An <see cref="IntPtr"/> to the IO map buffer.</param>
        /// <param name="slaveRxPdoOffsets">The calculated offsets of each slave's receive process data objects.</param>
        /// <param name="slaveTxPdoOffsets">The calculated offsets of each slave's transmit process data objects.</param>
        /// <param name="expectedWorkingCounter">The expected working counter.</param>
        /// <returns>Returns the status code of the unmanaged operation.</returns>
        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ConfigureIoMap(IntPtr context, IntPtr ioMapPtr, int[] slaveRxPdoOffsets, int[] slaveTxPdoOffsets, out int expectedWorkingCounter);

        /// <summary>
        /// Configures the distributed clocks system.
        /// </summary>
        /// <param name="frameCount">Number of frames to settle the control loop. Depending on the target time difference between the slaves, 15.000 frames are recommended.</param>
        /// <param name="targetTimeDifference">Maximum time between the reference clock and the slave clocks in nanoseconds (recommendation: 100 ns).</param>
        /// <param name="systemTimeDifference">The final maxmimum system time difference.</param>
        /// <returns>Returns the status code of the unmanaged operation.</returns>
        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ConfigureDc(IntPtr context, uint frameCount, uint targetTimeDifference, out uint systemTimeDifference);

        /// <summary>
        /// Configures SYNC0 / SYNC1.
        /// </summary>
        /// <param name="slaveIndex">The index of the corresponding slave.</param>
        /// <param name="assignActivate">The assign activate bytes from the EtherCAT slave information file.</param>
        /// <param name="assignActivateByteLength">The lengths of the assign activate bytes.</param>
        /// <param name="cycleTime0">The cycle time of SYNC0.</param>
        /// <param name="cycleTime1">The cycle time of SYNC1.</param>
        /// <param name="cycleShift">The shift time of SYNC0.</param>
        /// <returns>Returns the status code of the unmanaged operation.</returns>
        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ConfigureSync01(IntPtr context, ushort slaveIndex, ref byte[] assignActivate, int assignActivateByteLength, uint cycleTime0, uint cycleTime1, int cycleShift);

        /// <summary>
        /// Requests SAFE-OP state.
        /// </summary>
        /// <returns>Returns the status code of the unmanaged operation.</returns>
        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int CheckSafeOpState(IntPtr context);

        /// <summary>
        /// Requests OP state.
        /// </summary>
        /// <returns>Returns the status code of the unmanaged operation.</returns>
        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int RequestOpState(IntPtr context);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern void RegisterCallback(IntPtr context, UInt16 slaveIndex, [MarshalAs(UnmanagedType.FunctionPtr)]PO2SOCallback callback);

        #endregion

        #region "called during OP"

        /// <summary>
        /// Sends a frame to distribute new output process data and waits for return of this frame to receive input process data.
        /// </summary>
        /// <param name="dcTime">The current time of the reference slave.</param>
        /// <returns>Returns the status code of the unmanaged operation.</returns>
        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int UpdateIo(IntPtr context, out long dcTime);

        /// <summary>
        /// Compensates the clock drift of the reference slave.
        /// </summary>
        /// <param name="ticks">The number of ticks to be added to the slaves DC time offset register.</param>
        /// <returns></returns>
        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int CompensateDcDrift(IntPtr context, int ticks);

        /// <summary>
        /// Reads the current state of a single slave.
        /// </summary>
        /// <param name="slaveIndex">The slave index of the corresponding slave.</param>
        /// <param name="requestedState">The requested state.</param>
        /// <param name="slaveState">The actual state.</param>
        /// <param name="alStatusCode">The application layer status code.</param>
        /// <param name="systemTimeDifference">The distributed clocks system time difference.</param>
        /// <param name="speedCounterDifference">The distributed clocks speed counter difference.</param>
        /// <param name="outputPdoCount">The number of configured output PDOs.</param>
        /// <param name="inputPdoCount">The number of configured inout PDOs.</param>
        /// <returns></returns>
        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ReadSlaveState(IntPtr context, ushort slaveIndex, ref ushort requestedState, ref ushort slaveState, ref ushort alStatusCode, ref int systemTimeDifference, ref ushort speedCounterDifference, ref ushort outputPdoCount, ref ushort inputPdoCount);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern int ReadState(IntPtr context);

        #endregion

        #region "Debug"

        [SuppressUnmanagedCodeSecurity]
        [DllImport(EcShared.NATIVE_DLL_NAME)]
        public static extern UInt32 ReadAllRegisters(IntPtr context, UInt16 slaveIndex);

        #endregion
    }
}