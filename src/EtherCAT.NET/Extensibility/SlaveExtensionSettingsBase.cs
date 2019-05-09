﻿using EtherCAT.NET.Infrastructure;
using OneDas.Extensibility;
using System.Diagnostics.Contracts;
using System.Runtime.Serialization;

namespace EtherCAT.NET.Extensibility
{
    [DataContract]
    public abstract class SlaveExtensionSettingsBase : ExtensionSettingsBase
    {
        #region "Properties"

        public SlaveInfo SlaveInfo { get; set; }

        #endregion

        #region "Constructors"

        public SlaveExtensionSettingsBase(SlaveInfo slaveInfo)
        {
            Contract.Requires(slaveInfo != null);

            this.SlaveInfo = slaveInfo;
        }

        #endregion

        #region "Methods"

        public abstract void EvaluateSettings();

        public override void Validate()
        {
            base.Validate();
        }

        #endregion
    }
}
