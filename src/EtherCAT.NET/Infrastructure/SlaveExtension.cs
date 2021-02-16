using System.Collections.Generic;

namespace EtherCAT.NET.Infrastructure
{
    public abstract class SlaveExtension
    {
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
