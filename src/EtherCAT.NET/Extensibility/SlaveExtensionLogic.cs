using EtherCAT.NET.Infrastructure;
using OneDas.Extensibility;
using System.Collections.Generic;

namespace EtherCAT.NET.Extensibility
{
    public abstract class SlaveExtensionLogic : ExtensionLogicBase
    {
        public SlaveExtensionLogic(SlaveExtensionSettingsBase settings) : base(settings)
        {
            this.Settings = settings;
        }

        public new SlaveExtensionSettingsBase Settings { get; private set; }
        public abstract IEnumerable<SdoWriteRequest> GetSdoWriteRequests();
    }
}
