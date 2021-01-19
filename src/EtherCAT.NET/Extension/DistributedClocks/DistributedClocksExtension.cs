using EtherCAT.NET.Extensibility;
using EtherCAT.NET.Infrastructure;
using System.Collections.Generic;

namespace EtherCAT.NET.Extension
{
    public class DistributedClocksExtension : SlaveExtensionLogic
    {
        #region "Fields"

        DistributedClocksSettings _settings;

        #endregion

        #region "Constructors"

        public DistributedClocksExtension(DistributedClocksSettings settings) : base(settings)
        {
            _settings = settings;
        }

        #endregion

        #region "Methods"

        public override IEnumerable<SdoWriteRequest> GetSdoWriteRequests()
        {
            return new List<SdoWriteRequest> {  };
        }

        #endregion
    }
}