using EtherCAT.NET.Extensibility;
using EtherCAT.NET.Infrastructure;
using OneDas.Extensibility;
using System.Runtime.Serialization;

namespace EtherCAT.NET.Extension
{
    [DataContract]
    [ExtensionContext(typeof(EL6601Extension))]
    [ExtensionIdentification("EL6601", "EL6601", "Ethernet switch port terminal", @"WebClient.EL6601View.html", @"WebClient.EL6601.js")]
    [TargetSlave(0x00000002, 0x19C93052)]
    public class EL6601Settings : SlaveExtensionSettingsBase
    {
        #region "Constructors"

        public EL6601Settings(SlaveInfo slaveInfo) : base(slaveInfo)
        {
            //
        }

        #endregion

        #region "Methods"

        public override void EvaluateSettings()
        {
            //
        }

        #endregion
    }
}
