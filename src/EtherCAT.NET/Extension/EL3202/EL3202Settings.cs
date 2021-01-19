using EtherCAT.NET.Extensibility;
using EtherCAT.NET.Infrastructure;
using OneDas.Extensibility;
using System;
using System.Runtime.Serialization;

namespace EtherCAT.NET.Extension
{
    [DataContract]
    [ExtensionContext(typeof(EL3202Extension))]
    [ExtensionIdentification("EL3202", "EL3202", "2-channel input terminal PT100 (RTD) for 2- or 3-wire connection", "", "")]
    [TargetSlave(0x00000002, 0x0c823052)]
    public class EL3202Settings : SlaveExtensionSettingsBase
    {
        #region "Constructors"

        public EL3202Settings(SlaveInfo slaveInfo) : base(slaveInfo)
        {
            this.WiringMode = WiringMode.Wire2;
        }

        #endregion

        #region "Properties"

        [DataMember]
        public WiringMode WiringMode { get; set; }

        #endregion

        #region "Methods"

        public override void EvaluateSettings()
        {
            if (this.WiringMode == 0)
                throw new Exception(ExtensionErrorMessage.EL3202Settings_WiringModeInvalid);
        }

        #endregion
    }
}
