using EtherCAT.NET.Extensibility;
using EtherCAT.NET.Infrastructure;
using System;
using System.Collections.Generic;

namespace EtherCAT.NET.Extension
{
    public class EL3202Extension : SlaveExtensionLogic
    {
        #region "Fields"

        EL3202Settings _settings;

        #endregion

        #region "Constructors"

        public EL3202Extension(EL3202Settings settings) : base(settings)
        {
            _settings = settings;
        }

        #endregion

        #region "Methods"

        public override IEnumerable<SdoWriteRequest> GetSdoWriteRequests()
        {
            object[] data1;
            object[] data2;

            data1 = new object[] { (UInt16)_settings.WiringMode };
            data2 = new object[] { (UInt16)_settings.WiringMode };

            return new List<SdoWriteRequest> { new SdoWriteRequest(0x8000, 0x1A, data1), new SdoWriteRequest(0x8010, 0x1A, data2) };
        }

        #endregion
    }
}