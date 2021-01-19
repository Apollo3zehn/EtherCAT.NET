using EtherCAT.NET.Extensibility;
using EtherCAT.NET.Infrastructure;
using System;
using System.Collections.Generic;

namespace EtherCAT.NET.Extension
{
    public class EL6601Extension : SlaveExtensionLogic
    {
        #region "Fields"

        EL6601Settings _settings;

        #endregion

        #region "Constructors"
        
        public EL6601Extension(EL6601Settings settings) : base(settings)
        {
            _settings = settings;
        }

        #endregion

        #region "Methods"

        public override IEnumerable<SdoWriteRequest> GetSdoWriteRequests()
        {
            object[] R_0xF800;
            object[] R_0x1A00;
            object[] R_0x1680;
            object[] R_0x1A80;
            object[] R_0x1C12;
            object[] R_0x1C13;
            object[] R_0x6000;
            object[] R_0x6002;
            object[] R_0x6003;
            object[] R_0x8000;

            // Device Configuration Data (0xF800…0xF8FF)
            R_0xF800 = new object[] { (UInt16)0x0100 };

            // PDO configuration
            R_0x1680 = new object[] { (UInt16)0x0001, (byte)0x10, (byte)0x02, (byte)0x00, (byte)0xF1 }; // PDO control
            R_0x1A00 = new object[] { (UInt16)0x0003, // Var 1
                                      (byte)0x10, (byte)0x03, (UInt16)0x6000,
                                      (byte)0x10, (byte)0x04, (UInt16)0x6000,
                                      (byte)0x10, (byte)0x05, (UInt16)0x6000 };
            R_0x1A80 = new object[] { (UInt16)0x0001, (byte)0x10, (byte)0x01, (byte)0x00, (byte)0xF1 }; // PDO status

            // PDO Assign objects (0x1C12, 0x1C13)
            R_0x1C12 = new object[] { (UInt16)0x0001, (UInt16)0x1680 };
            R_0x1C13 = new object[] { (UInt16)0x0002, (UInt16)0x1A00, (UInt16)0x1A80 };

            // Receiving Frame Data
            R_0x6000 = new object[] { (UInt16)0x0005,
                                      (byte)0x0A, (byte)0xFC, (byte)0x81, (byte)0x04, (byte)0x01, (byte)0x01, (UInt16)0x0001, (byte)0x00, (byte)0x00, (byte)0x00, (byte)0x00, (byte)0x00, (byte)0x00  };

            // Receiving Frame Identification (Ignore Item Net Var Subscriber)
            R_0x6002 = new object[] { (UInt16)0x0005,
                                      (byte)0x01, (byte)0x00, (byte)0x01, (byte)0x01, (byte)0x01 };

            // Receiving Frame Length (Area Length Nat Var Subscriber)
            R_0x6003 = new object[] { (UInt16)0x0005,
                                      (UInt16)0x0006, (UInt16)0x0002, (UInt16)0x0002, (UInt16)0x0002, (UInt16)0x0002 };

            // Configuration Data object area (0x8000…0x8FFF)
            R_0x8000 = new object[] { (byte)0x03 };

            return new List<SdoWriteRequest>
            {
                new SdoWriteRequest(0xF800, 0x02, R_0xF800),
                new SdoWriteRequest(0x8000, 0x04, R_0x8000),
                new SdoWriteRequest(0x6003, 0x00, R_0x6003),
                new SdoWriteRequest(0x6000, 0x00, R_0x6000),
                new SdoWriteRequest(0x6002, 0x00, R_0x6002),

                new SdoWriteRequest(0x1A00, 0x00, R_0x1A00),
                new SdoWriteRequest(0x1680, 0x00, R_0x1680),
                new SdoWriteRequest(0x1A80, 0x00, R_0x1A80),

                new SdoWriteRequest(0x1C12, 0x00, R_0x1C12),
                new SdoWriteRequest(0x1C13, 0x00, R_0x1C13),
            };
        }

        #endregion
    }
}