using System.Collections.Generic;

namespace EtherCAT.NET.Infrastructure
{
    public abstract class SlaveExtension
    {
        #region "Properties"

        public SlaveInfo Slave { get; set; }

        #endregion

        #region "Constructors"

        public SlaveExtension(SlaveInfo slave)
        {
            this.Slave = slave;
        }

        #endregion

        #region "Methods"

        public abstract void EvaluateSettings();

        public abstract IEnumerable<SdoWriteRequest> GetSdoWriteRequests();

        public virtual void Validate()
        {
            //
        }

        #endregion
    }
}
