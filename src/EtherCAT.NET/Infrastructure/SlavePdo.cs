using System.Collections.Generic;

namespace EtherCAT.NET.Infrastructure
{
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

        public string Name { get; private set; }

        public ushort Index { get; private set; }

        public ushort OsMax { get; private set; }

        public bool IsFixed { get; private set; }

        public bool IsMandatory { get; private set; }

        public int SyncManager { get; set; }

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