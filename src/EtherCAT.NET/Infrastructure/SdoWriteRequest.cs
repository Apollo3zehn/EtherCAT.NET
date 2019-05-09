using System.Collections.Generic;

namespace EtherCAT.NET.Infrastructure
{
    public struct SdoWriteRequest
    {
        #region "Fields"

        public ushort Index;
        public byte SubIndex;
        public IEnumerable<object> Dataset;

        #endregion

        #region "Constructors"

        public SdoWriteRequest(ushort index, byte subIndex, IEnumerable<object> dataset)
        {
            this.Index = index;
            this.SubIndex = subIndex;
            this.Dataset = dataset;
        }

        #endregion
    }
}