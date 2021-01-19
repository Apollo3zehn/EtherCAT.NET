using System;
using System.Collections.Generic;
using System.Linq;

namespace EtherCAT.NET.Infrastructure
{
    /// <summary>
    /// Represents an EtherCAT slave.
    /// </summary>
    public class SlaveInfo
    {
        #region "Fields"

        private IEnumerable<SlaveExtension> _slaveExtensions;

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

            this.Extensions = new List<SlaveExtension>();
        }

        #endregion

        #region "Properties"

        public uint Manufacturer { get; private set; }

        public uint ProductCode { get; private set; }

        public uint Revision { get; private set; }

        public ushort OldCsa { get; set; }

        public ushort Csa { get; private set; }

        public List<SlaveInfo> Children { get; private set; }

        public List<SlaveExtension> Extensions { get; set; }

        public EtherCATInfoDescriptionsDevice Esi { get; set; }

        public EtherCATInfoDescriptionsGroup EsiGroup { get; set; }

        public SlaveInfoDynamicData DynamicData { get; set; }

        #endregion

        #region "Methods"

        public IEnumerable<SlaveInfo> Descendants()
        {
            return this.DescendantsInternal();
        }

        private List<SlaveInfo> DescendantsInternal()
        {
            var slaves = new List<SlaveInfo>();

            this.Children.ToList().ForEach(Child =>
            {
                slaves.Add(Child);
                slaves.AddRange(Child.Descendants());
            });

            return slaves;
        }

        public IEnumerable<SlaveVariable> GetVariables()
        {
            return this.DynamicData.Pdos.SelectMany(x => x.Variables);
        }

        public void Validate()
        {
            if (this.Csa == 0)
                throw new Exception(ErrorMessage.SlaveInfo_IdInvalid);

            this.Extensions
                .ToList()
                .ForEach(slaveExtension => slaveExtension
                .Validate());

            // Improve: validate also SlavePdo and SlaveVariable
            // Improve: validate also SlaveInfoDynamicData
        }

        // configuration
        public IEnumerable<SdoWriteRequest> GetConfiguration(IEnumerable<SlaveExtension> slaveExtensions)
        {
            _slaveExtensions = slaveExtensions.ToList();

            return this.GetSdoConfiguration().Concat(this.GetPdoConfiguration()).Concat(this.GetSmConfiguration());
        }

        private IEnumerable<SdoWriteRequest> GetSdoConfiguration()
        {
            return _slaveExtensions.ToList().SelectMany(slaveExtensionLogic => slaveExtensionLogic.GetSdoWriteRequests()).ToList();
        }

        private IEnumerable<SdoWriteRequest> GetPdoConfiguration()
        {
            if (this.Esi.Mailbox?.CoE.PdoConfig == true)
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
            if (this.Esi.Mailbox?.CoE.PdoAssign == true)
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