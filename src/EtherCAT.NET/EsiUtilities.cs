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
            EsiUtilities.SourceEtherCatInfos = new List<EtherCATInfo>();
        }

        #endregion

        #region I/O

        private static EtherCATInfo LoadEsi(string esiFileName)
        {
            EtherCATInfo etherCatInfo;
            var xmlSerializer = new XmlSerializer(typeof(EtherCATInfo));

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
            var xmlSerializer = new XmlSerializer(typeof(EtherCATInfo));

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
            var infos = new List<EtherCATInfo>();
            var filePaths = EsiUtilities.EnumerateFiles(_cacheDirectoryPath, ".xml", SearchOption.AllDirectories).ToList();

            foreach (var filePath in filePaths)
            {
                try
                {
                    infos.Add(EsiUtilities.LoadEsi(filePath));
                }
                catch (Exception)
                {
                    // TODO: write warning into logger
                }
            }

            EsiUtilities.CacheEtherCatInfos = infos;
        }

        private static void LoadEsiSource(string sourceDirectoryPath)
        {
            var infos = new List<EtherCATInfo>();
            var filePaths = EsiUtilities.EnumerateFiles(sourceDirectoryPath, ".xml", SearchOption.AllDirectories);

            foreach (var filePath in filePaths)
            {
                try
                {
                    infos.Add(EsiUtilities.LoadEsi(filePath));
                }
                catch (Exception)
                {
                    // TODO: write warning into logger
                }
            }

            EsiUtilities.SourceEtherCatInfos = infos;
        }

        #endregion

        #region Methods

        public static IEnumerable<string> EnumerateFiles(string directoryPath, string searchPatternExpression, SearchOption searchOption)
        {
            var regex = new Regex(searchPatternExpression, RegexOptions.IgnoreCase);

            return Directory
                .EnumerateFiles(directoryPath, "*", searchOption)
                .Where(x => regex.IsMatch(x));
        }

        private static (EtherCATInfo etherCATInfo, EtherCATInfoDescriptionsDevice device, EtherCATInfoDescriptionsGroup group) TryFindDevice(List<EtherCATInfo> etherCATInfos, uint manufacturer, uint productCode, uint revision)
        {
            EtherCATInfo info = null;
            EtherCATInfoDescriptionsDevice device = null;

            foreach (var currentInfo in etherCATInfos)
            {
                var vendorId = (uint)EsiUtilities.ParseHexDecString(currentInfo.Vendor.Id);

                if (vendorId != manufacturer)
                    continue;

                device = currentInfo.Descriptions.Devices.FirstOrDefault(currentDevice =>
                {
                    var found = !string.IsNullOrWhiteSpace(currentDevice.Type.ProductCode) &&
                                !string.IsNullOrWhiteSpace(currentDevice.Type.RevisionNo) &&
                                (int)EsiUtilities.ParseHexDecString(currentDevice.Type.ProductCode) == productCode &&
                                (int)EsiUtilities.ParseHexDecString(currentDevice.Type.RevisionNo) == revision;

                    if (found)
                        info = currentInfo;

                    return found;
                });

                if (device != null)
                    break;
            }

            // try to find old revision
            if (device == null)
            {
                etherCATInfos.ToList().ForEach(currentInfo =>
                {
                    device = currentInfo.Descriptions.Devices.Where(currentDevice =>
                    {
                        var found = !string.IsNullOrWhiteSpace(currentDevice.Type.ProductCode) && 
                                    !string.IsNullOrWhiteSpace(currentDevice.Type.RevisionNo) &&
                                    (int)EsiUtilities.ParseHexDecString(currentDevice.Type.ProductCode) == productCode;

                        if (found)
                            info = currentInfo;

                        return found;
                    }).OrderBy(currentDevice => currentDevice.Type.RevisionNo).LastOrDefault();
                });

                // return without success
                if (device == null)
                    return (null, null, null);
            }

            // find group
            if (info == null)
                throw new Exception($"ESI entry for group type '{device}' not found.");

            var group = info.Descriptions.Groups.FirstOrDefault(currentGroup => currentGroup.Type == device.GroupType);

            if (group == null)
                throw new Exception($"ESI entry for group type '{device}' not found.");

            return (info, device, group);
        }

        public static void ResetCache()
        {
            lock (_lock)
            {
                foreach (var file in new DirectoryInfo(_cacheDirectoryPath).GetFiles())
                {
                    file.Delete();
                }

                EsiUtilities.CacheEtherCatInfos = new List<EtherCATInfo>();
                EsiUtilities.SourceEtherCatInfos = new List<EtherCATInfo>();
            }
        }

        public static (EtherCATInfoDescriptionsDevice device, EtherCATInfoDescriptionsGroup group) FindEsi(string esiSourceDirectoryPath, uint manufacturer, uint productCode, uint revision)
        {
            // try to find ESI in cache
            (_, var device, var group) = EsiUtilities.TryFindDevice(EsiUtilities.CacheEtherCatInfos, manufacturer, productCode, revision);

            if (device == null)
            {
                // update cache
                EsiUtilities.UpdateCache(esiSourceDirectoryPath, manufacturer, productCode, revision);

                // try to find ESI in cache again
                (_, device, group) = EsiUtilities.TryFindDevice(EsiUtilities.CacheEtherCatInfos, manufacturer, productCode, revision);

                // it finally failed
                if (device == null)
                    throw new Exception($"Could not find ESI information of manufacturer '0x{manufacturer:X}' for slave with product code '0x{productCode:X}' and revision '0x{revision:X}'.");
            }

            return (device, group);
        }

        public static List<DistributedClocksOpMode> GetOpModes(this SlaveInfo slaveInfo)
        {
            return slaveInfo.SlaveEsi.Dc.OpMode
                .Select(opMode => new DistributedClocksOpMode(opMode))
                .ToList();
        }

        public static long ParseHexDecString(string value)
        {
            if (value.StartsWith("#x"))
                return uint.Parse(value.Replace("#x", string.Empty), NumberStyles.HexNumber);
            else
                return long.Parse(value);
        }

        private static bool UpdateCache(string esiSourceDirectoryPath, uint manufacturer, uint productCode, uint revision)
        {
            // check if source ESI files have been loaded
            if (!EsiUtilities.SourceEtherCatInfos.Any())
                EsiUtilities.LoadEsiSource(esiSourceDirectoryPath);

            // try to find requested device info
            (var sourceInfo, var sourceDevice, _) = EsiUtilities.TryFindDevice(EsiUtilities.SourceEtherCatInfos, manufacturer, productCode, revision);

            if (sourceDevice == null)
                return false;

            lock (_lock)
            {
                // find matching EtherCATInfo in cache
                var cacheInfo = EsiUtilities.CacheEtherCatInfos.FirstOrDefault(current =>
                {
                    var vendorId = (uint)EsiUtilities.ParseHexDecString(current.Vendor.Id);
                    return vendorId == manufacturer;
                });

                // extend cache file
                if (cacheInfo != null)
                {
                    // add new groups
                    var cacheGroups = cacheInfo.Descriptions.Groups.ToList();

                    foreach (var sourceGroup in sourceInfo.Descriptions.Groups)
                    {
                        if (!cacheGroups.Any(current => current.Type == sourceGroup.Type))
                        {
                            cacheGroups.Add(sourceGroup);
                        }
                    }

                    cacheInfo.Descriptions.Groups = cacheGroups.ToArray();

                    // add found device
                    cacheInfo.Descriptions.Devices = cacheInfo.Descriptions.Devices.ToList().Concat(new[] { sourceDevice }).ToArray();
                }
                // create new cache file
                else
                {
                    cacheInfo = sourceInfo;
                    cacheInfo.Descriptions.Devices = new EtherCATInfoDescriptionsDevice[] { sourceDevice };

                    EsiUtilities.CacheEtherCatInfos.Add(cacheInfo);
                }

                // save new/updated EtherCATInfo to disk
                EsiUtilities.SaveEsi(cacheInfo, Path.Combine(_cacheDirectoryPath, $"{cacheInfo.Vendor.Id}.xml"));
            }

            // return
            return true;
        }

        #endregion

        #region Properties

        private static List<EtherCATInfo> CacheEtherCatInfos { get; set; }

        private static List<EtherCATInfo> SourceEtherCatInfos { get; set; }

        #endregion
    }
}
