//using EtherCAT.NET.Infrastructure;
//using EtherCAT.NET.Extensibility;
//using OneDas.Extensibility;
//using OneDas.Infrastructure;
//using System;

//namespace EtherCAT.NET.Extension
//{
//    public class DistributedClocksSupporter : IExtensionSupporter
//    {
//        #region "Fields"

//        private IOneDasSerializer _oneDasSerializer;
//        private IExtensionFactory _extensionFactory;

//        #endregion

//        #region "Constructors"

//        public DistributedClocksSupporter(IExtensionFactory extensionFactory, IOneDasSerializer oneDasSerializer)
//        {
//            _extensionFactory = extensionFactory;
//            _oneDasSerializer = oneDasSerializer;
//        }

//        #endregion

//        #region "Methods"

//        public void Initialize()
//        {
//            //
//        }

//        public ActionResponse HandleActionRequest(ActionRequest actionRequest)
//        {
//            object returnData;
//            SlaveInfo slaveInfo;

//            switch (actionRequest.MethodName)
//            {
//                case "GetOpModes":

//                    slaveInfo = _oneDasSerializer.Deserialize<SlaveInfo>(actionRequest.Data);
//                    slaveInfo.Validate();

//                    ExtensibilityHelper.CreateDynamicData(_extensionFactory, slaveInfo);

//                    returnData = slaveInfo.GetOpModes();

//                    break;

//                default:

//                    throw new ArgumentException("unknown method name");
//            }

//            return new ActionResponse(returnData);
//        }

//        #endregion
//    }
//}
