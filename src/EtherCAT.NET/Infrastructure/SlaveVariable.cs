using OneDas;
using OneDas.Extensibility;
using OneDas.Infrastructure;
using System.Diagnostics.Contracts;
using System.Runtime.Serialization;

namespace EtherCAT.Infrastructure
{
    [DataContract]
    public class SlaveVariable : DataPort
    {
        #region "Constructors"

        public SlaveVariable(SlavePdo parent, string name, ushort index, byte subIndex, DataDirection dataDirection, OneDasDataType dataType, byte bitLength = 0) : base(name, dataType, dataDirection, Endianness.LittleEndian)
        {
            Contract.Requires(parent != null);

            this.Parent = parent;
            this.Index = index;
            this.SubIndex = subIndex;

            if (bitLength == 0)
            {
                this.BitLength = OneDasUtilities.GetBitLength(dataType, false);
            }
            else
            {
                this.BitLength = bitLength;
            }
        }

        #endregion

        #region "Properties"

        /// <summary>
        /// Gets the parent SlavePdo of the SlaveVariable/>.
        /// </summary>
        /// <returns>Returns the parent SlavePdo of the SlaveVariable</returns>
        public SlavePdo Parent { get; private set; }

        /// <summary>
        /// Gets the index of the SlaveVariable/>.
        /// </summary>
        /// <returns>Returns the index of the SlaveVariable</returns>
        [DataMember]
        public ushort Index { get; private set; }

        /// <summary>
        /// Gets the data sub index of the SlaveVariable/>.
        /// </summary>
        /// <returns>Returns the sub index of the SlaveVariable</returns>
        [DataMember]
        public byte SubIndex { get; private set; }

        /// <summary>
        /// Gets the bit length of the SlaveVariable/>.
        /// </summary>
        /// <returns>Returns the bit length of the SlaveVariable</returns>
        [DataMember]
        public byte BitLength { get; private set; }

        #endregion

        #region "Methods"

        public override string GetId()
        {
            return $"{ this.Parent.Parent.Csa } / { this.Parent.Name } / { this.Name }";
        }

        #endregion
    }
}