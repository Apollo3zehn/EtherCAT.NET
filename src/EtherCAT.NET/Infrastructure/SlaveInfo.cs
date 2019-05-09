using EtherCAT.NET.Extensibility;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.Serialization;

namespace EtherCAT.NET.Infrastructure
{
    /// <summary>
    /// Represents an EtherCAT slave.
    /// </summary>
    [DataContract(IsReference = true)]
    public class SlaveInfo
    {
        #region "Events"

        public delegate void ActivePdoSetChangedEventHandler(object sender, EventArgs e);

        #endregion

        #region "Fields"

        private IEnumerable<SlaveExtensionLogic> _slaveExtensionLogicSet;

        #endregion

        #region "Constructors"

        public SlaveInfo(ec_slave_info_t slaveIdentification) : this(slaveIdentification, new List<SlaveInfo>())
        {
            //
        }

        public SlaveInfo(ec_slave_info_t slaveIdentification, List<SlaveInfo> childSet)
        {
            Contract.Requires(childSet != null, nameof(childSet));

            this.Manufacturer = slaveIdentification.manufacturer;
            this.ProductCode = slaveIdentification.productCode;
            this.Revision = slaveIdentification.revision;
            this.OldCsa = slaveIdentification.oldCsa;
            this.Csa = slaveIdentification.csa;
            this.ChildSet = childSet;

            this.SlaveExtensionSet = new List<SlaveExtensionSettingsBase>();
        }

        #endregion

        #region "Properties"

        [DataMember]
        public uint Manufacturer { get; private set; }

        [DataMember]
        public uint ProductCode { get; private set; }

        [DataMember]
        public uint Revision { get; private set; }

        [DataMember]
        public ushort OldCsa { get; set; }

        [DataMember]
        public ushort Csa { get; private set; }

        [DataMember]
        public List<SlaveInfo> ChildSet { get; private set; }

        [DataMember]
        public IEnumerable<SlaveExtensionSettingsBase> SlaveExtensionSet { get; set; }

        public EtherCATInfoDescriptionsDevice SlaveEsi { get; set; }

        public EtherCATInfoDescriptionsGroup SlaveEsi_Group { get; set; }

        public SlaveInfoDynamicData DynamicData { get; set; }

        #endregion

        #region "Methods"

        /// <summary>
        /// Collects all descendants of this SlaveInfo.
        /// </summary>
        /// <returns>Returns all descendants of this SlaveInfo.</returns>
        public IEnumerable<SlaveInfo> Descendants()
        {
            return this.DescendantsInternal();
        }

        private List<SlaveInfo> DescendantsInternal()
        {
            List<SlaveInfo> functionReturnValue = default;

            functionReturnValue = new List<SlaveInfo>();
            this.ChildSet.ToList().ForEach(Child =>
            {
                functionReturnValue.Add(Child);
                functionReturnValue.AddRange(Child.Descendants());
            });

            return functionReturnValue;
        }

        /// <summary>
        /// Collects all variables of this SlaveInfo.
        /// </summary>
        /// <returns>Returns all variables of this SlaveInfo.</returns>
        public IEnumerable<SlaveVariable> GetVariableSet()
        {
            return this.DynamicData.PdoSet.SelectMany(x => x.VariableSet);
        }

        public void Validate()
        {
            if (this.Csa == 0)
            {
                throw new Exception(ErrorMessage.SlaveInfo_IdInvalid);
            }

            this.SlaveExtensionSet.ToList().ForEach(slaveExtension => slaveExtension.Validate());

            // Improve: validate also SlavePdo and SlaveVariable
            // Improve: validate also SlaveInfoDynamicData
        }

        // configuration
        public IEnumerable<SdoWriteRequest> GetConfiguration(IEnumerable<SlaveExtensionLogic> slaveExtensionLogicSet)
        {
            _slaveExtensionLogicSet = slaveExtensionLogicSet.ToList();

            return this.GetSdoConfiguration().Concat(this.GetPdoConfiguration()).Concat(this.GetSmConfiguration());
        }

        private IEnumerable<SdoWriteRequest> GetSdoConfiguration()
        {
            return _slaveExtensionLogicSet.ToList().SelectMany(slaveExtensionLogic => slaveExtensionLogic.GetSdoWriteRequestSet()).ToList();
        }

        private IEnumerable<SdoWriteRequest> GetPdoConfiguration()
        {
            if (this.SlaveEsi.Mailbox?.CoE.PdoConfig == true)
            {
                return this.DynamicData.PdoSet.Where(slavePdo => !slavePdo.IsFixed).Select(slavePdo =>
                {
                    List<object> dataset;
                    List<SlaveVariable> slaveVariableSet;

                    dataset = new List<object>();
                    slaveVariableSet = slavePdo.VariableSet.ToList();

                    dataset.Add((byte)slaveVariableSet.Count());

                    foreach (SlaveVariable slaveVariable in slavePdo.VariableSet)
                    {
                        dataset.Add((ushort)(slaveVariable.SubIndex + (Convert.ToUInt16(slaveVariable.BitLength) << 8)));
                        dataset.Add(slaveVariable.Index);
                    }

                    return new SdoWriteRequest(slavePdo.Index, 0x00, dataset);
                });
            }
            else
            {
                return new List<SdoWriteRequest>();
            }
        }

        private IEnumerable<SdoWriteRequest> GetSmConfiguration()
        {
            if (this.SlaveEsi.Mailbox?.CoE.PdoAssign == true)
            {
                return Enumerable.Range(2, 2).Select(syncManager =>
                {
                    List<object> dataset;
                    List<SlavePdo> slavePdoSet;

                    dataset = new List<object>();
                    slavePdoSet = this.DynamicData.PdoSet.Where(x => x.SyncManager == syncManager).ToList();

                    //
                    dataset.Add((byte)slavePdoSet.Count());

                    foreach (SlavePdo slavePdo in slavePdoSet)
                    {
                        dataset.Add(slavePdo.Index);
                    }

                    return new SdoWriteRequest((ushort)(0x1c10 + syncManager), 0, dataset);
                });
            }
            else
            {
                return new List<SdoWriteRequest>();
            }
        }

        #endregion
    }
}