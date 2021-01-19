using EtherCAT.NET.Extensibility;
using System;
using System.Collections.Generic;
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

        private IEnumerable<SlaveExtensionLogic> _slaveExtensionLogics;

        #endregion

        #region "Constructors"

        public SlaveInfo(ec_slave_info_t slaveIdentification) : this(slaveIdentification, new List<SlaveInfo>())
        {
            //
        }

        public SlaveInfo(ec_slave_info_t slaveIdentification, List<SlaveInfo> children)
        {
            this.Manufacturer = slaveIdentification.manufacturer;
            this.ProductCode = slaveIdentification.productCode;
            this.Revision = slaveIdentification.revision;
            this.OldCsa = slaveIdentification.oldCsa;
            this.Csa = slaveIdentification.csa;
            this.Children = children;

            this.SlaveExtensions = new List<SlaveExtensionSettingsBase>();
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
        public List<SlaveInfo> Children { get; private set; }

        [DataMember]
        public IEnumerable<SlaveExtensionSettingsBase> SlaveExtensions { get; set; }

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
            var functionReturnValue = new List<SlaveInfo>();

            this.Children.ToList().ForEach(Child =>
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
        public IEnumerable<SlaveVariable> GetVariables()
        {
            return this.DynamicData.Pdos.SelectMany(x => x.Variables);
        }

        public void Validate()
        {
            if (this.Csa == 0)
            {
                throw new Exception(ErrorMessage.SlaveInfo_IdInvalid);
            }

            this.SlaveExtensions
                .ToList()
                .ForEach(slaveExtension => slaveExtension
                .Validate());

            // Improve: validate also SlavePdo and SlaveVariable
            // Improve: validate also SlaveInfoDynamicData
        }

        // configuration
        public IEnumerable<SdoWriteRequest> GetConfiguration(IEnumerable<SlaveExtensionLogic> slaveExtensionLogics)
        {
            _slaveExtensionLogics = slaveExtensionLogics.ToList();

            return this.GetSdoConfiguration().Concat(this.GetPdoConfiguration()).Concat(this.GetSmConfiguration());
        }

        private IEnumerable<SdoWriteRequest> GetSdoConfiguration()
        {
            return _slaveExtensionLogics.ToList().SelectMany(slaveExtensionLogic => slaveExtensionLogic.GetSdoWriteRequests()).ToList();
        }

        private IEnumerable<SdoWriteRequest> GetPdoConfiguration()
        {
            if (this.SlaveEsi.Mailbox?.CoE.PdoConfig == true)
            {
                return this.DynamicData.Pdos
                    .Where(slavePdo => !slavePdo.IsFixed)
                    .Select(slavePdo =>
                {
                    var slaveVariables = slavePdo.Variables.ToList();

                    var dataset = new List<object>();
                    dataset.Add((byte)slaveVariables.Count());

                    foreach (SlaveVariable slaveVariable in slavePdo.Variables)
                    {
                        var lowBytes = slaveVariable.Index;
                        var highBytes = (ushort)(slaveVariable.SubIndex + (Convert.ToUInt16(slaveVariable.BitLength) << 8));

                        var bytes = BitConverter.GetBytes(highBytes);
                        Array.Reverse(bytes);
                        highBytes = BitConverter.ToUInt16(bytes, 0);

                        dataset.Add((uint)(highBytes + (lowBytes << 16)));
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
                    var slavePdos = this.DynamicData.Pdos
                        .Where(x => x.SyncManager == syncManager)
                        .ToList();

                    var dataset = new List<object>();
                    dataset.Add((byte)slavePdos.Count());

                    foreach (SlavePdo slavePdo in slavePdos)
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