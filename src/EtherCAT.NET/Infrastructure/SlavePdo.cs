using System.Collections.Generic;
using System.Runtime.Serialization;

namespace EtherCAT.NET.Infrastructure
{
    [DataContract]
    public class SlavePdo
    {
        #region "Constructors"

        public SlavePdo(SlaveInfo parent, string name, ushort index, ushort osMax, bool isFixed, bool isMandatory, int syncManager)
        {
            this.Parent = parent;
            this.Name = name;
            this.Index = index;
            this.OsMax = osMax;
            this.IsFixed = isFixed;
            this.IsMandatory = isMandatory;
            this.SyncManager = syncManager;
        }

        #endregion

        #region "Properties"

        public SlaveInfo Parent { get; private set; }

        [DataMember]
        public string Name { get; private set; }

        [DataMember]
        public ushort Index { get; private set; }

        public ushort OsMax { get; private set; }

        public bool IsFixed { get; private set; }

        public bool IsMandatory { get; private set; }

        [DataMember]
        public int SyncManager { get; set; }

        [DataMember]
        public IList<SlaveVariable> Variables { get; private set; }

        #endregion

        #region "Methods"

        public void SetVariables(IList<SlaveVariable> variables)
        {
            this.Variables = variables;
        }

        #endregion
    }
}