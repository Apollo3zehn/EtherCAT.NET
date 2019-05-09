using EtherCAT.NET.Infrastructure;
using EtherCAT.NET.Extension;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace EtherCAT.NET
{
    public static class EsiUtilities
    {
        #region Fields

        private static object _lock;
        private static string _cacheDirectoryPath;

        #endregion

        #region Constructors

        static EsiUtilities()
        {
            _lock = new object();
            _cacheDirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EtherCAT.NET", "Cache");

            Directory.CreateDirectory(_cacheDirectoryPath);

            EsiUtilities.LoadEsiCache();
            EsiUtilities.SourceEtherCatInfoSet = new List<EtherCATInfo>();
        }

        #endregion

        #region I/O

        private static EtherCATInfo LoadEsi(string esiFileName)
        {
            XmlSerializer xmlSerializer;
            EtherCATInfo etherCatInfo;

            xmlSerializer = new XmlSerializer(typeof(EtherCATInfo));

            using (StreamReader streamReader = new StreamReader(esiFileName))
            {
                try
                {
                    etherCatInfo = (EtherCATInfo)xmlSerializer.Deserialize(streamReader);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Could not open file {esiFileName}. Reason: {ex.Message}");
                }
            }

            etherCatInfo.Descriptions.Devices.ToList().ForEach(y => y.RxPdo = y.RxPdo ?? new PdoType[] { });
            etherCatInfo.Descriptions.Devices.ToList().ForEach(y => y.RxPdo.ToList().ForEach(z => z.Entry = z.Entry ?? new PdoTypeEntry[] { }));

            etherCatInfo.Descriptions.Devices.ToList().ForEach(y => y.TxPdo = y.TxPdo ?? new PdoType[] { });
            etherCatInfo.Descriptions.Devices.ToList().ForEach(y => y.TxPdo.ToList().ForEach(z => z.Entry = z.Entry ?? new PdoTypeEntry[] { }));

            return etherCatInfo;
        }

        private static void SaveEsi(EtherCATInfo etherCATInfo, string esiFileName)
        {
            XmlSerializer xmlSerializer;

            xmlSerializer = new XmlSerializer(typeof(EtherCATInfo));

            lock (_lock)
            {
                using (StreamWriter streamWriter = new StreamWriter(esiFileName))
                {
                    try
                    {
                        xmlSerializer.Serialize(streamWriter, etherCATInfo);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Could not write file {esiFileName}. Reason: {ex.Message}");
                    }
                }
            }
        }

        private static void LoadEsiCache()
        {
            List<EtherCATInfo> infoSet;
            List<string> filePathSet;

            infoSet = new List<EtherCATInfo>();
            filePathSet = EsiUtilities.EnumerateFiles(_cacheDirectoryPath, ".xml", SearchOption.AllDirectories).ToList();

            foreach (var filePath in filePathSet)
            {
                try
                {
                    infoSet.Add(EsiUtilities.LoadEsi(filePath));
                }
                catch (Exception)
                {
                    // TODO: write warning into logger
                }
            }

            EsiUtilities.CacheEtherCatInfoSet = infoSet;
        }

        private static void LoadEsiSource(string sourceDirectoryPath)
        {
            List<EtherCATInfo> infoSet;
            IEnumerable<string> filePathSet;

            infoSet = new List<EtherCATInfo>();
            filePathSet = EsiUtilities.EnumerateFiles(sourceDirectoryPath, ".xml", SearchOption.AllDirectories);

            foreach (var filePath in filePathSet)
            {
                try
                {
                    infoSet.Add(EsiUtilities.LoadEsi(filePath));
                }
                catch (Exception)
                {
                    // TODO: write warning into logger
                }
            }

            EsiUtilities.SourceEtherCatInfoSet = infoSet;
        }

        #endregion

        #region Methods

        public static IEnumerable<string> EnumerateFiles(string directoryPath, string searchPatternExpression, SearchOption searchOption)
        {
            Regex regex;

            regex = new Regex(searchPatternExpression, RegexOptions.IgnoreCase);

            return Directory.EnumerateFiles(directoryPath, "*", searchOption).Where(x => regex.IsMatch(x));
        }

        private static (EtherCATInfo etherCATInfo, EtherCATInfoDescriptionsDevice device, EtherCATInfoDescriptionsGroup group) TryFindDevice(List<EtherCATInfo> etherCATInfoSet, uint manufacturer, uint productCode, uint revision)
        {
            EtherCATInfo info;
            EtherCATInfoDescriptionsDevice device;
            EtherCATInfoDescriptionsGroup group;

            info = null;
            device = null;

            foreach (var currentInfo in etherCATInfoSet)
            {
                uint vendorId;

                vendorId = uint.Parse(currentInfo.Vendor.Id.Replace("#x", string.Empty), NumberStyles.HexNumber);

                if (vendorId != manufacturer)
                {
                    continue;
                }

                device = currentInfo.Descriptions.Devices.FirstOrDefault(currentDevice =>
                {
                    bool found;

                    found = !string.IsNullOrWhiteSpace(currentDevice.Type.ProductCode) &&
                            !string.IsNullOrWhiteSpace(currentDevice.Type.RevisionNo) &&
                             Int32.Parse(currentDevice.Type.ProductCode.Substring(2), NumberStyles.HexNumber) == productCode &&
                             Int32.Parse(currentDevice.Type.RevisionNo.Substring(2), NumberStyles.HexNumber) == revision;

                    if (found)
                    {
                        info = currentInfo;
                    }

                    return found;
                });

                if (device != null)
                {
                    break;
                }
            }

            // try to find old revision
            if (device == null)
            {
                etherCATInfoSet.ToList().ForEach(currentInfo =>
                {
                    device = currentInfo.Descriptions.Devices.Where(currentDevice =>
                    {
                        bool found;

                        found = !string.IsNullOrWhiteSpace(currentDevice.Type.ProductCode) && 
                                !string.IsNullOrWhiteSpace(currentDevice.Type.RevisionNo) &&
                                 Int32.Parse(currentDevice.Type.ProductCode.Substring(2), NumberStyles.HexNumber) == productCode;

                        if (found)
                        {
                            info = currentInfo;
                        }

                        return found;
                    }).OrderBy(currentDevice => currentDevice.Type.RevisionNo).LastOrDefault();
                });

                // return without success
                if (device == null)
                {
                    return (null, null, null);
                }
            }

            // find group
            group = info.Descriptions.Groups.FirstOrDefault(currentGroup => currentGroup.Type == device.GroupType);

            if (group == null)
            {
                throw new Exception($"ESI entry for group type '{device}' not found.");
            }

            return (info, device, group);
        }

        private static bool UpdateCache(string esiSourceDirectoryPath, uint manufacturer, uint productCode, uint revision)
        {
            EtherCATInfo cacheInfo;
            EtherCATInfo sourceInfo;
            EtherCATInfoDescriptionsDevice sourceDevice;
            List<EtherCATInfoDescriptionsGroup> cacheGroupSet;

            // check if source ESI files have been loaded
            if (!EsiUtilities.SourceEtherCatInfoSet.Any())
            {
                EsiUtilities.LoadEsiSource(esiSourceDirectoryPath);
            }

            // try to find requested device info
            (sourceInfo, sourceDevice, _) = EsiUtilities.TryFindDevice(EsiUtilities.SourceEtherCatInfoSet, manufacturer, productCode, revision);

            if (sourceDevice == null)
            {
                return false;
            }

            lock (_lock)
            {
                // find matching EtherCATInfo in cache
                cacheInfo = EsiUtilities.CacheEtherCatInfoSet.FirstOrDefault(current =>
                {
                    uint vendorId;

                    vendorId = current.Vendor.Id.Length > 2 ? uint.Parse(current.Vendor.Id.Substring(2), NumberStyles.HexNumber) : uint.Parse(current.Vendor.Id, NumberStyles.HexNumber);
                    return vendorId == manufacturer;
                });

                // extend cache file
                if (cacheInfo != null)
                {
                    // add new groups
                    cacheGroupSet = cacheInfo.Descriptions.Groups.ToList();

                    foreach (var sourceGroup in sourceInfo.Descriptions.Groups)
                    {
                        if (!cacheGroupSet.Any(current => current.Type == sourceGroup.Type))
                        {
                            cacheGroupSet.Add(sourceGroup);
                        }
                    }

                    cacheInfo.Descriptions.Groups = cacheGroupSet.ToArray();

                    // add found device
                    cacheInfo.Descriptions.Devices = cacheInfo.Descriptions.Devices.ToList().Concat(new[] { sourceDevice }).ToArray();
                }
                // create new cache file
                else
                {
                    cacheInfo = sourceInfo;
                    cacheInfo.Descriptions.Devices = new EtherCATInfoDescriptionsDevice[] { sourceDevice };

                    EsiUtilities.CacheEtherCatInfoSet.Add(cacheInfo);
                }

                // save new/updated EtherCATInfo to disk
                EsiUtilities.SaveEsi(cacheInfo, Path.Combine(_cacheDirectoryPath, $"{cacheInfo.Vendor.Id}.xml"));
            }

            // return
            return true;
        }

        public static void ResetCache()
        {
            lock (_lock)
            {
                foreach (var file in new DirectoryInfo(_cacheDirectoryPath).GetFiles())
                {
                    file.Delete();
                }

                EsiUtilities.CacheEtherCatInfoSet = new List<EtherCATInfo>();
                EsiUtilities.SourceEtherCatInfoSet = new List<EtherCATInfo>();
            }
        }

        public static (EtherCATInfoDescriptionsDevice device, EtherCATInfoDescriptionsGroup group) FindEsi(string esiSourceDirectoryPath, uint manufacturer, uint productCode, uint revision)
        {
            EtherCATInfoDescriptionsDevice device;
            EtherCATInfoDescriptionsGroup group;

            // try to find ESI in cache
            (_, device, group) = EsiUtilities.TryFindDevice(EsiUtilities.CacheEtherCatInfoSet, manufacturer, productCode, revision);

            if (device == null)
            {
                // update cache
                EsiUtilities.UpdateCache(esiSourceDirectoryPath, manufacturer, productCode, revision);

                // try to find ESI in cache again
                (_, device, group) = EsiUtilities.TryFindDevice(EsiUtilities.CacheEtherCatInfoSet, manufacturer, productCode, revision);

                // it finally failed
                if (device == null)
                {
                    throw new Exception($"Could not find ESI information of manufacturer '0x{manufacturer:X}' for slave with product code '0x{productCode:X}' and revision '0x{revision:X}'.");
                }
            }

            return (device, group);
        }

        public static List<DistributedClocksOpMode> GetOpModes(this SlaveInfo slaveInfo)
        {
            return slaveInfo.SlaveEsi.Dc.OpMode.Select(opMode => new DistributedClocksOpMode(opMode)).ToList();
        }

        #endregion

        #region Properties

        private static List<EtherCATInfo> CacheEtherCatInfoSet { get; set; }
        private static List<EtherCATInfo> SourceEtherCatInfoSet { get; set; }

        #endregion
    }
}
