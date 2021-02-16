using System.Collections.Generic;
using EtherCAT.NET.Infrastructure;

namespace EtherCAT.NET.Extension
{
    /// <summary>
    /// This class is a slave extension in order to set SDO write requests. This
    /// requests are returned by function GetConfiguration from SlaveInfo class.
    /// </summary>
    public class InitialSettingsExtension : SlaveExtension
    {
        public InitialSettingsExtension(IEnumerable<SdoWriteRequest> requests)
        {
            _sdoRequests = requests;
        }

        private IEnumerable<SdoWriteRequest> _sdoRequests { get; set; }

        public override void EvaluateSettings()
        {
            
        }

        public override IEnumerable<SdoWriteRequest> GetSdoWriteRequests()
        {
            return _sdoRequests;
        }
    }
}
