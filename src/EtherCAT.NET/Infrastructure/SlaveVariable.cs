using System;

namespace EtherCAT.NET.Infrastructure
{
    public class SlaveVariable
    {
        #region "Constructors"

        public SlaveVariable(SlavePdo parent, string name, ushort index, byte subIndex, DataDirection dataDirection, EthercatDataType dataType, byte bitLength = 0)
        {
            this.Parent = parent;
            this.Name = name;
            this.Index = index;
            this.SubIndex = subIndex;
            this.DataDirection = dataDirection;
            this.DataType = dataType;

            if (bitLength == 0)
                this.BitLength = EcUtilities.GetBitLength(dataType);
            else
                this.BitLength = bitLength;
        }

        #endregion

        #region "Properties"

        public IntPtr DataPtr { get; set; }

        public int BitOffset { get; set; }

        public SlavePdo Parent { get; private set; }

        public string Name { get; private set; }

        public ushort Index { get; private set; }

        public byte SubIndex { get; private set; }

        public EthercatDataType DataType { get; private set; }

        public DataDirection DataDirection { get; private set; }

        public byte BitLength { get; private set; }

        #endregion
    }
}