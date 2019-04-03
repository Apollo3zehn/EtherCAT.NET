using EtherCAT.Extensibility;
using EtherCAT.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EtherCAT.Extension
{
    public class EL6731_0010Extension : SlaveExtensionLogic
    {
        #region "Fields"

        EL6731_0010Settings _settings;

        #endregion

        #region "Constructors"
        
        public EL6731_0010Extension(EL6731_0010Settings settings) : base(settings)
        {
            _settings = settings;
        }

        #endregion

        #region "Methods"

        public override IEnumerable<SdoWriteRequest> GetSdoWriteRequestSet()
        {
            object[] R_0x8000;

            if (_settings.SelectedModuleSet.Count == 0)
            {
                throw new Exception($"At least one module must be selected for terminal EL6731-0010 ({ _settings.SlaveInfo.Csa }).");
            }

            // cfgData
            List<byte> cfgData = new List<byte>();

            _settings.SelectedModuleSet.ToList().ForEach(x =>
            {
                if ((int)x > 0xff)
                {
                    cfgData.Add(Convert.ToByte(((int)x & 0xff00) >> 8));
                }

                cfgData.Add(Convert.ToByte((int)x & 0xff));
            });

            // prmData
            byte[] userPrmData = new byte[] { };
            byte[] prmData = new byte[]
            {
                0x80,
                0x1,
                0x14,
                0xb,
                0x9,
                0x5f,
                0x0,
                0x80,
                0x0,
                0x8
            }.Concat(userPrmData).ToArray();

            R_0x8000 = new byte[]
            {
                0x2d,
                0x0,
                Convert.ToByte(_settings.StationNumber),
                0x0,
                0x0,
                0x0,
                0x0,
                0x0,
                0x0,
                0x0,
                0x0,
                0x0,
                0x0,
                0x0,
                0xa8,
                0x1,
                0x0,
                0x0,
                0xf4,
                0xf0,
                0x0,
                0x0,
                0x0,
                0x0,
                Convert.ToByte(prmData.Length),
                Convert.ToByte(cfgData.Count),
                0x0,
                0x0,
                0x0,
                0x0
            }.Concat(prmData).Concat(cfgData).Cast<object>().ToArray();

            return new List<SdoWriteRequest>
            {
                new SdoWriteRequest(0x8000, 0x00, R_0x8000),
            };
        }

        #endregion
    }
}