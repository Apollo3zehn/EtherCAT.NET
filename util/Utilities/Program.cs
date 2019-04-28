using EtherCAT;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Utilities
{
    public static class Program
    {
        static void Main(string[] args)
        {
            ConsoleKeyInfo consoleKeyInfo;

            Console.WriteLine("Compile ESI files");
            Console.WriteLine();
            Console.WriteLine("[1] Beckhoff | [2] Gantner | [3] Anybus");
            Console.WriteLine();

            consoleKeyInfo = Console.ReadKey(true);

            try
            {
                switch (consoleKeyInfo.Key)
                {
                    case ConsoleKey.NumPad1:
                    case ConsoleKey.D1:
                        Program.CompileEcSlaveEsi(Vendor.Beckhoff);
                        break;
                    case ConsoleKey.NumPad2:
                    case ConsoleKey.D2:
                        Program.CompileEcSlaveEsi(Vendor.Gantner);
                        break;
                    case ConsoleKey.NumPad3:
                    case ConsoleKey.D3:
                        Program.CompileEcSlaveEsi(Vendor.Anybus);
                        break;
                }

                Console.WriteLine("ESI file compilation finished.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to continue.");
            Console.ReadKey(true);
        }

        public static void CompileEcSlaveEsi(Vendor vendor)
        {
            List<string> slaveNameSet = new List<string>();
            IEnumerable<string> fileNameSet = default;
            string targetfileName = null;

            switch (vendor)
            {
                case Vendor.Beckhoff:

                    slaveNameSet.Add("EK1100");
                    slaveNameSet.Add("EK1501");
                    slaveNameSet.Add("EK1521");
                    slaveNameSet.Add("EL1008");
                    slaveNameSet.Add("EL2008");
                    slaveNameSet.Add("EL3051");
                    slaveNameSet.Add("EL3054");
                    slaveNameSet.Add("EL3061");
                    slaveNameSet.Add("EL3064");
                    slaveNameSet.Add("EL3104");
                    slaveNameSet.Add("EL3154");
                    slaveNameSet.Add("EL3202");
                    slaveNameSet.Add("EL3214");
                    slaveNameSet.Add("EL3356");
                    slaveNameSet.Add("EL3632");
                    slaveNameSet.Add("EL3681");
                    slaveNameSet.Add("EL3751");
                    slaveNameSet.Add("EL3773");
                    slaveNameSet.Add("EL6601");
                    slaveNameSet.Add("EL6695");
                    slaveNameSet.Add("EL6695-0002");
                    slaveNameSet.Add("EL6731");
                    slaveNameSet.Add("EL6731-0010");
                    slaveNameSet.Add("EL6751-0010");
                    slaveNameSet.Add("EL9011");
                    slaveNameSet.Add("EL9100");
                    slaveNameSet.Add("EL9410");
                    slaveNameSet.Add("EL9505");

                    fileNameSet = EsiUtilities.EnumerateFiles(@"C:\TwinCAT\3.1\Config\Io\EtherCAT", "(.*Beckhoff E[K|L].*\\.xml)", SearchOption.TopDirectoryOnly).ToList();
                    targetfileName = "Beckhoff Automation.xml";

                    break;

                case Vendor.Gantner:

                    slaveNameSet.Add("Q.bloxx-EC A107");
                    slaveNameSet.Add("Q.bloxx-EC BC");
                    slaveNameSet.Add("Q.station 101");

                    fileNameSet = EsiUtilities.EnumerateFiles(@".\ESI\Gantner", ".*", SearchOption.AllDirectories).ToList();
                    targetfileName = "Gantner Instruments.xml";

                    break;

                case Vendor.Anybus:

                    slaveNameSet.Add("Anybus X-gateway - Slave");

                    fileNameSet = EsiUtilities.EnumerateFiles(@".\ESI\Anybus", ".*", SearchOption.AllDirectories).ToList();
                    targetfileName = "Anybus.xml";

                    break;

                default:

                    throw new NotSupportedException();
            }

            XDocument ecSlaveEsiStorage = XDocument.Load(fileNameSet.First());
            XElement XGroups = ecSlaveEsiStorage.Root.Element("Descriptions").Element("Groups");
            XElement XDevices = ecSlaveEsiStorage.Root.Element("Descriptions").Element("Devices");

            ecSlaveEsiStorage.Root.Element("Descriptions").Element("Devices").Elements("Device").Remove();
            ecSlaveEsiStorage.Root.Element("Descriptions").Element("Groups").Elements("Group").Remove();
            ecSlaveEsiStorage.Root.Element("Descriptions").Elements("Modules").Remove();

            XDevices.RemoveNodes();

            foreach (string fileName in fileNameSet)
            {
                foreach (XElement XGroup in XDocument.Load(fileName).Root.Element("Descriptions").Element("Groups").Elements())
                {
                    XGroups.Add(XGroup);
                }

                foreach (XElement XDevice in XDocument.Load(fileName).Root.Element("Descriptions").Element("Devices").Elements().Where((x, y) => slaveNameSet.Contains(x.Element("Type").Value)))
                {
                    XDevices.Add(XDevice);
                }
            }

            XElement XElement_Descriptions = ecSlaveEsiStorage.Root.Element("Descriptions");
            XElement_Descriptions.Element("Groups").Remove();

            XElement_Descriptions.AddFirst(new XElement(XGroups.Name, XGroups.Elements().GroupBy(x =>
            {
                if (x.Attribute("SortOrder") != null)
                {
                    return x.Element("Type").Value;
                }
                else
                {
                    return x.Element("Name").Value;
                }
            }).Select(groupedGroups =>
            {
                XElement group = groupedGroups.Where(y => y.Element("ImageData16x14") != null).FirstOrDefault();

                if (group == null)
                {
                    group = groupedGroups.First();
                }

                return group;
            }).OrderBy(x =>
            {
                if (x.Attribute("SortOrder") != null)
                {
                    return x.Element("Type").Value;
                }
                else
                {
                    return x.Element("Name").Value;
                }
            }).ToList()));

            // bug fix
            if (vendor == Vendor.Beckhoff)
            {
                IEnumerable<XElement> XDeviceSet = ecSlaveEsiStorage.Descendants("Device").Where(x => x.Element("Type").Attribute("ProductCode")?.Value == "#xe303052" || x.Element("Type").Attribute("ProductCode")?.Value == "#x0e303052");
                XDeviceSet.ToList().ForEach(x => x.Elements("TxPdo").Where(y => y.Element("Index").Value == "#x1a41").First().Element("Entry").Element("Index").Value = "#x6011");
                XDeviceSet.ToList().ForEach(x => x.Elements("TxPdo").Where(y => y.Element("Index").Value == "#x1a41").First().Element("Entry").Element("SubIndex").Value = "1");
            }

            ecSlaveEsiStorage.Save($@"{ Environment.GetFolderPath(Environment.SpecialFolder.Desktop) }\{ targetfileName }");
        }
    }
}