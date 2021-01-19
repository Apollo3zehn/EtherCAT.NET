using System.Collections.Generic;

namespace EtherCAT.NET.Infrastructure
{
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

        public string Name { get; set; }

        public string Description { get; set; }

        public IList<SlavePdo> Pdos { get; private set; }

        public byte[] Base64ImageData { get; private set; }

        #endregion
    }
}