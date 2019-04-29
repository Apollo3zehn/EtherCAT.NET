using EtherCAT.Infrastructure;
using EtherCAT.Extension;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace EtherCAT
{
    public static class EsiUtilities
    {
        public static IEnumerable<string> EnumerateFiles(string directoryPath, string searchPatternExpression, SearchOption searchOption)
        {
            Regex regex;

            regex = new Regex(searchPatternExpression, RegexOptions.IgnoreCase);

            return Directory.EnumerateFiles(directoryPath, "*", searchOption).Where(x => regex.IsMatch(x));
        }

        public static IEnumerable<EtherCATInfo> EtherCatInfoSet { get; private set; }

        public static List<DistributedClocksOpMode> GetOpModes(this SlaveInfo slaveInfo)
        {
            return slaveInfo.SlaveEsi.Dc.OpMode.Select(opMode => new DistributedClocksOpMode(opMode)).ToList();
        }

        public static void InitializeEsi(string slaveEsiSourceDirectoryPath)
        {
            XmlSerializer xmlSerializer;
            IEnumerable<string> slaveEsiFileNameSet;

            xmlSerializer = new XmlSerializer(typeof(EtherCATInfo));
            slaveEsiFileNameSet = EsiUtilities.EnumerateFiles(slaveEsiSourceDirectoryPath, ".*", SearchOption.AllDirectories);

            EsiUtilities.EtherCatInfoSet = slaveEsiFileNameSet.ToList().Select(x =>
            {
                EtherCATInfo etherCatInfo = default;

                using (StreamReader streamReader = new StreamReader(x))
                {
                    try
                    {
                        etherCatInfo = (EtherCATInfo)xmlSerializer.Deserialize(streamReader);
                    }
                    catch
                    {
                        throw new Exception($"Could not open file {x}.");
                    }
                }

                etherCatInfo.Descriptions.Devices.ToList().ForEach(y => y.RxPdo = y.RxPdo ?? new PdoType[] { });
                etherCatInfo.Descriptions.Devices.ToList().ForEach(y => y.RxPdo.ToList().ForEach(z => z.Entry = z.Entry ?? new PdoTypeEntry[] { }));

                etherCatInfo.Descriptions.Devices.ToList().ForEach(y => y.TxPdo = y.TxPdo ?? new PdoType[] { });
                etherCatInfo.Descriptions.Devices.ToList().ForEach(y => y.TxPdo.ToList().ForEach(z => z.Entry = z.Entry ?? new PdoTypeEntry[] { }));

                return etherCatInfo;
            }).ToList();
        }

        public static (EtherCATInfoDescriptionsDevice slaveEsi, EtherCATInfoDescriptionsGroup slaveEsi_Group) FindEsi(uint manufacturer, uint productCode, uint revision)
        {
            EtherCATInfo etherCatInfo = EsiUtilities.EtherCatInfoSet.Where(y =>
            {
                uint vendorId = y.Vendor.Id.Length > 2 ? uint.Parse(y.Vendor.Id.Substring(2), NumberStyles.HexNumber) : uint.Parse(y.Vendor.Id, NumberStyles.HexNumber);
                return vendorId == manufacturer;
            }).FirstOrDefault();

            if (etherCatInfo == null)
            {
                throw new Exception($"ESI file for manufacturer '0x{manufacturer:X}' not found.");
            }

            EtherCATInfoDescriptionsDevice slaveEsi = etherCatInfo?.Descriptions.Devices.Where(y =>
            {
                if (!string.IsNullOrWhiteSpace(y.Type.ProductCode) && !string.IsNullOrWhiteSpace(y.Type.RevisionNo))
                    return Int32.Parse(y.Type.ProductCode.Substring(2), NumberStyles.HexNumber) == productCode && Int32.Parse(y.Type.RevisionNo.Substring(2), NumberStyles.HexNumber) == revision;
                else
                    return false;
            }).FirstOrDefault();

            // try to find older revision
            if (slaveEsi == null)
            {
                slaveEsi = etherCatInfo?.Descriptions.Devices.Where(y =>
                {
                    if (!string.IsNullOrWhiteSpace(y.Type.ProductCode) && !string.IsNullOrWhiteSpace(y.Type.RevisionNo))
                    {
                        return Int32.Parse(y.Type.ProductCode.Substring(2), NumberStyles.HexNumber) == productCode;
                    }
                    else
                    {
                        return false;
                    }
                }).LastOrDefault();
            }

            if (slaveEsi == null)
            {
                throw new Exception($"ESI file for slave with product code '0x{productCode:X}' and revision '0x{revision:X}' not found.");
            }

            EtherCATInfoDescriptionsGroup slaveEsi_Group = etherCatInfo?.Descriptions.Groups.Where((y) => y.Type == slaveEsi.GroupType).First();

            if (slaveEsi_Group == null)
            {
                throw new Exception($"ESI group for group type '{ slaveEsi.GroupType }' not found.");
            }

            return (slaveEsi, slaveEsi_Group);
        }
    }
}
