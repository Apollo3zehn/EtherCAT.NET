using EtherCAT;
using EtherCAT.NET.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OneDas.Extensibility;
using SOEM.PInvoke;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SampleMaster
{
    class Program
    {
        static async Task Main(string[] args)
        {
            /* Copy native file. NOT required in end user scenarios, where this package is installed via NuGet! */
            Directory.EnumerateFiles("./runtimes/", "*.dll", SearchOption.AllDirectories).ToList().ForEach(filePath =>
            {
                if (filePath.Contains("win-x64"))
                {
                    File.Copy(filePath, Path.GetFileName(filePath), true);
                }
            });

            /* prepare dependency injection */
            var services = new ServiceCollection();

            ConfigureServices(services);

            /* create types */
            var provider = services.BuildServiceProvider();
            var extensionFactory = provider.GetRequiredService<IExtensionFactory>();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();

            /* create EtherCAT master settings (with 10 Hz cycle frequency) */
            var cycleFrequency = 10U;
            var esiDirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ESI");
            var settings = new EcSettings(cycleFrequency, esiDirectoryPath, "106530387D67");

            /* create hierachical slave info */
            // TODO: simplify
            EsiUtilities.InitializeEsi(settings.EsiDirectoryPath);

            var context = EcHL.CreateContext();
            var slaveInfo = EcUtilities.ScanDevices(EcHL.CreateContext(), settings.NicHardwareAddress);

            slaveInfo.Descendants().ToList().ForEach(current =>
            {
                ExtensibilityHelper.CreateDynamicData(extensionFactory, current);
            });

            EcHL.FreeContext(context);

            /* create EC Master */
            using (var master = new EcMaster(slaveInfo, settings, extensionFactory, loggerFactory.CreateLogger("EtherCAT Master")))
            {
                master.Configure();

                /* create variable references */
                var variableSet = slaveInfo.Descendants().SelectMany(child => child.GetVariableSet()).ToList();

                /* start master */
                var random = new Random();
                var cts = new CancellationTokenSource();

                var task = Task.Run(() =>
                {
                    var sleepTime = 1000 / (int)cycleFrequency;

                    while (!cts.IsCancellationRequested)
                    {
                        master.UpdateIo(DateTime.UtcNow);

                        unsafe
                        {
                            // TODO: simplify
                            if (variableSet.Any())
                            {
                                var myVariableSpan = new Span<int>(variableSet.First().DataPtr.ToPointer(), 1);
                                myVariableSpan[0] = random.Next(0, 100);
                            }
                        }

                        Thread.Sleep(sleepTime);
                    }
                }, cts.Token);

                /* wait for stop signal */
                Console.ReadKey();

                cts.Cancel();
                await task;
            }
        }

        static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IExtensionFactory, ExtensionFactory>();

            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.SetMinimumLevel(LogLevel.Debug);
                loggingBuilder.AddConsole();
            });
        }
    }
}
