using System;
namespace EtherCAT.NET.Infrastructure
{
    public enum SlaveState : ushort
    {
        // No valid state.
        None = 0x00,
        // Init state*
        Init = 0x01,
        // Pre-operational. 
        PreOp = 0x02,
        // Boot state
        Boot = 0x03,
        // Safe-operational.
        SafeOp = 0x04,
        // Operational 
        Operational = 0x08,
        // Error or ACK error 
        Ack = 0x10,
        Error = 0x10
    }
}
