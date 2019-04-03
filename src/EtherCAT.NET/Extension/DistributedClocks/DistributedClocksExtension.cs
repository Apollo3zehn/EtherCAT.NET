using EtherCAT.Extensibility;
using EtherCAT.Infrastructure;
using System.Collections.Generic;

namespace EtherCAT.Extension
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

        public override IEnumerable<SdoWriteRequest> GetSdoWriteRequestSet()
        {
            return new List<SdoWriteRequest> {  };
        }

        #endregion
    }
}