using EtherCAT.Extensibility;
using EtherCAT.Infrastructure;
using System.Collections.Generic;

namespace EtherCAT.Extension
{
    public class AnybusXGatewayExtension : SlaveExtensionLogic
    {
        #region "Fields"

        AnybusXGatewaySettings _settings;

        #endregion

        #region "Constructors"

        public AnybusXGatewayExtension(AnybusXGatewaySettings settings) : base(settings)
        {
            _settings = settings;
        }

        #endregion

        #region "Methods"

        public override IEnumerable<SdoWriteRequest> GetSdoWriteRequestSet()
        {
            return new List<SdoWriteRequest>();
        }

        #endregion
    }
}