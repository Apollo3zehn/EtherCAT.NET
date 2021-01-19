using EtherCAT.NET;
using Microsoft.DotNet.PlatformAbstractions;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SampleMaster
{
    class Program
    {
        static async Task Main(string[] args)
        {          
            /* Set interface name. Edit this to suit your needs. */
            var interfaceName = "Lokal";

            /* Set ESI location. Make sure it contains ESI files! The default path is /home/{user}/.local/share/ESI */
            var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var esiDirectoryPath = Path.Combine(localAppDataPath, "ESI");
            Directory.CreateDirectory(esiDirectoryPath);

            /* Copy native file. NOT required in end user scenarios, where EtherCAT.NET package is installed via NuGet! */
            var codeBase = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            Directory.EnumerateFiles(Path.Combine(codeBase, "runtimes"), "*soem_wrapper.*", SearchOption.AllDirectories).ToList().ForEach(filePath =>
            {
                if (filePath.Contains(RuntimeEnvironment.RuntimeArchitecture))
                    File.Copy(filePath, Path.Combine(codeBase, Path.GetFileName(filePath)), true);
            });

            /* create logger */
            var loggerFactory = LoggerFactory.Create(loggingBuilder =>
            {
                loggingBuilder.SetMinimumLevel(LogLevel.Debug);
                loggingBuilder.AddConsole();
            });

            var logger = loggerFactory.CreateLogger("EtherCAT Master");

            /* create EtherCAT master settings (with 10 Hz cycle frequency) */
            var settings = new EcSettings(cycleFrequency: 10U, esiDirectoryPath, interfaceName);

            /* scan available slaves */
            var rootSlave = EcUtilities.ScanDevices(settings.InterfaceName);

            rootSlave.Descendants().ToList().ForEach(slave =>
            {
                // If you have special extensions for this slave, add it here:                    
                // slave.Extensions.Add(new MyFancyExtension());

                EcUtilities.CreateDynamicData(settings.EsiDirectoryPath, slave);
            });

            /* print list of slaves */
            var message = new StringBuilder();
            var slaves = rootSlave.Descendants().ToList();

            message.AppendLine($"Found {slaves.Count()} slaves:");

            foreach (var slave in slaves)
            {
                message.AppendLine($"{slave.DynamicData.Name} (PDOs: {slave.DynamicData.Pdos.Count} - CSA: {slave.Csa})");
            }

            logger.LogInformation(message.ToString().TrimEnd());

            /* create variable references for later use */
            var variables = slaves.SelectMany(child => child.GetVariables()).ToList();

            /* create EC Master */
            using (var master = new EcMaster(settings, logger))
            {
                // If you want to change SDO values of a certain slave, register a callback:
                //
                // EcHL.RegisterCallback(master.Context, slaveIndex /* 1-based indexing */, slaveIndex =>
                // {
                //     var returnValue = EcUtilities.SdoWrite(master.Context, slaveIndex, ...);
                //     EcUtilities.CheckErrorCode(master.Context, returnValue);
                //     return 0;
                // });

                try
                {
                    master.Configure(rootSlave);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                    throw;
                }

                /* start master */
                var random = new Random();
                var cts = new CancellationTokenSource();

                var task = Task.Run(() =>
                {
                    var sleepTime = 1000 / (int)settings.CycleFrequency;

                    while (!cts.IsCancellationRequested)
                    {
                        master.UpdateIO(DateTime.UtcNow);

                        unsafe
                        {
                            if (variables.Any())
                            {
                                var myVariableSpan = new Span<int>(variables.First().DataPtr.ToPointer(), 1);
                                myVariableSpan[0] = random.Next(0, 100);
                            }
                        }

                        Thread.Sleep(sleepTime);
                    }
                }, cts.Token);

                /* wait for stop signal */
                Console.ReadKey(true);

                cts.Cancel();
                await task;
            }
        }
    }
}
