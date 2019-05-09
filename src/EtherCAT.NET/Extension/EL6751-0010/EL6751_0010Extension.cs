using EtherCAT.NET.Extensibility;
using EtherCAT.NET.Infrastructure;
using System;
using System.Collections.Generic;

namespace EtherCAT.NET.Extension
{
    public class EL6751_0010Extension : SlaveExtensionLogic
    {
        #region "Fields"

        EL6751_0010Settings _settings;

        #endregion

        #region "Constructors"
        
        public EL6751_0010Extension(EL6751_0010Settings settings) : base(settings)
        {
            _settings = settings;
        }

        #endregion

        #region "Methods"

        public override IEnumerable<SdoWriteRequest> GetSdoWriteRequestSet()
        {
            Byte localNodeAddress = 0x01;
            //Byte remoteNodeAddress = 0x01;

            object[] R_0x1C32;
            object[] R_0x1C33;
            object[] R_0xF800;
            object[] R_0x8000;
            object[] R_0x8006;
            object[] R_0x8008;
            object[] R_0x1C12;
            object[] R_0x1C13;

            // SM output parameter
            R_0x1C32 = new object[]
            {
                (UInt32)0x00989680 // Cycle time
            };

            // SM output parameter
            R_0x1C33 = new object[]
            {
                (UInt32)0x005B8D80 // Shift time
            };

            // Device Configuration Data (0xF800…0xF8FF)
            R_0xF800 = new object[]
            {
                (UInt16)0x0011,
                (Byte)localNodeAddress, // Node Address
                (Byte)0x02,             // Baudrate
                (UInt16)0x0080,         // COB ID of SYNC Message
                (UInt32)0x00002710,     // SYNC cycle time
                (UInt32)0x00000000,     // Bustiming registers
                (Byte)0x01,             // bit_1: Slave Mode; bit_2: PDO Align 8 Bytes; rest: reserved
                (Byte)0x1E,             // TxPDO Delay
                (UInt16)0x0064,         // CAN message queue size
                (Byte)0x00,             // reserved
                (Byte)0x00,             // reserved
                (UInt16)0x0000,         // reserved
                (UInt32)0x00000000,     // reserved
                (UInt32)0x00000000,     // reserved
                (UInt32)0x00000000,     // reserved
                (UInt32)0x00000000,     // reserved
                (UInt32)0x00000000,     // reserved
                (UInt32)0x00000000      // reserved
            };

            // Configuration Data object area (0x8000…0x8FFF)
            R_0x8000 = new object[] // 74 bytes
            {
                (UInt16)0x002E,
                (UInt16)localNodeAddress,                           // Node address
                (UInt32)0x00000405,                                 // Device type
                (UInt32)0x00000002,                                 // Vendor ID
                (UInt32)0x00001A5F,                                 // Product code
                (UInt32)0x00000000,                                 // Revision
                (UInt32)0x00000000,                                 // Serial number
                (UInt16)0x0000,                                     // Network flags
                (UInt16)0x0000,                                     // Network port
                (Byte)0x00, (Byte)0x00, (Byte)0x00,
                (Byte)0x00, (Byte)0x00, (Byte)0x00,                 // Network segment address
                (UInt16)0x0002,                                     // Flags
                (UInt16)0x0064,                                     // Guarding time
                (UInt16)0x0003,                                     // Life time factor
                (UInt16)0x0000,                                     // SDO timeout
                (UInt16)0x07D0,                                     // Boot timeout
                (Byte)0x05,                                         // Parallel AoE services
                (Byte)0x00,                                         // bit_1: Reaction on CANopen fault; bit_2: Restart behaviour after CANopen fault; 
                                                                    // bit_3: Master reaction after CANopen fault; bit_4: Changes of CAN TxPDOs after CANopen fault; 
                                                                    // rest: reserved
                (Byte)0x0A,                                         // reserved
                (Byte)0x00,                                         // reserved
                (UInt32)0x0000,                                     // reserved
                (UInt32)0x0000,                                     // reserved
                (UInt32)0x0000,                                     // reserved
                (UInt32)0x0000,                                     // reserved
                (UInt32)0x0000,                                     // reserved
                (UInt32)0x0000,                                     // reserved
                (UInt32)0x0000                                      // reserved
            };

            // TX-PDOs
            R_0x8006 = new object[]
            {
                (UInt16)0x0000,
                //(UInt16)0x0001,

                //// for each module
                //(UInt32)0x00000180 + localNodeAddress,              // COB Id 0 0x180 + Node Address
                //(Byte)0xFF,                                         // Transmission Type
                //(Byte)0x04,                                         // Byte Length
                //(UInt16)0x0000,                                     // Inhibit Time
                //(UInt16)0x0000,                                     // Event Time
                //(UInt16)0x0800,                                     // Flags
            };

            // RX-PDOs
            R_0x8008 = new object[]
            {
                (UInt16)0x0000,
                //(UInt16)0x0001,

                //// for each module
                //(UInt32)0x00000200 + remoteNodeAddress,             // COB Id 0 0x200 + remote Node Address
                //(Byte)0xFF,                                         // Transmission Type
                //(Byte)0x01,                                         // Byte Length
                //(UInt16)0x0000,                                     // Inhibit Time
                //(UInt16)0x0000,                                     // Event Time
                //(UInt16)0x0000,                                     // Flags
            };

            // TxPDO Mapping
            R_0x1C12 = new object[]
            {
                (UInt16)0x0000,
            };

            // RxPDO Mapping
            R_0x1C13 = new object[]
            {
                (UInt16)0x0002,
                //(UInt16)0x1A00,
                (UInt16)0x1A83,
                (UInt16)0x1A84
            };

            // Eventuell kann SOEM nicht mit variablen PDO umgehen. Inhalt von 0x1A00 (Daten) ist leer. Berechnung der Sync Manager Länge schlägt fehl.

            return new List<SdoWriteRequest>
            {
                new SdoWriteRequest(0x1C32, 0x02, R_0x1C32),
                new SdoWriteRequest(0x1C33, 0x03, R_0x1C33),
                new SdoWriteRequest(0xF800, 0x00, R_0xF800),
                new SdoWriteRequest(0x8000, 0x00, R_0x8000),
                new SdoWriteRequest(0x8006, 0x00, R_0x8006),
                new SdoWriteRequest(0x8008, 0x00, R_0x8008),
                new SdoWriteRequest(0x1C12, 0x00, R_0x1C12),
                new SdoWriteRequest(0x1C13, 0x00, R_0x1C13),
            };
        }

        #endregion
    }
}