using EtherCAT.Extensibility;
using EtherCAT.Infrastructure;
using System.Collections.Generic;

namespace EtherCAT.Extension
{
    public class QBloxxEcA107Extension : SlaveExtensionLogic
    {
        #region "Fields"

        QBloxxEcA107Settings _settings;

        #endregion

        #region "Constructors"

        public QBloxxEcA107Extension(QBloxxEcA107Settings settings) : base(settings)
        {
            _settings = settings;
        }

        #endregion

        #region "Methods"

        public override IEnumerable<SdoWriteRequest> GetSdoWriteRequestSet()
        {
            return new List<SdoWriteRequest>();
        }

        #endregion
    }
}