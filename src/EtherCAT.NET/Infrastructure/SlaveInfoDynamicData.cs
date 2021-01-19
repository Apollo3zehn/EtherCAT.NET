using System.Collections.Generic;
using System.Runtime.Serialization;

namespace EtherCAT.NET.Infrastructure
{
    [DataContract]
    public class SlaveInfoDynamicData
    {
        #region "Constructors"

        public SlaveInfoDynamicData(string name, string description, IList<SlavePdo> pdos, byte[] base64ImageData)
        {
            this.Name = name;
            this.Description = description;
            this.Pdos = pdos;
            this.Base64ImageData = base64ImageData;
        }

        #endregion

        #region "Properties"

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public IList<SlavePdo> Pdos { get; private set; }

        [DataMember]
        public byte[] Base64ImageData { get; private set; }

        #endregion
    }
}