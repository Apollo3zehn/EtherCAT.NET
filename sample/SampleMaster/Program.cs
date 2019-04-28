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
            // Copy native file. NOT required in end user scenarios.
            Directory.EnumerateFiles("./runtimes/", "*.dll", SearchOption.AllDirectories).ToList().ForEach(filePath =>
            {
                if (filePath.Contains("win-x64"))
                {
                    File.Copy(filePath, Path.GetFileName(filePath), true);
                }
            });

            // dependency injection
            var services = new ServiceCollection();

            ConfigureServices(services);

            // create types
            var provider = services.BuildServiceProvider();
            var extensionFactory = provider.GetRequiredService<IExtensionFactory>();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();

            // create EtherCAT master settings (with 10 Hz cycle frequency)
            var cycleFrequency = 10U;
            var settings = new EcSettings(cycleFrequency, "./ESI", "8C89A55782D5");

            // create RootSlaveInfo
            // TODO: simplify
            var context = EcHL.CreateContext();
            var slaveInfo = EcUtilities.ScanDevices(EcHL.CreateContext(), settings.NicHardwareAddress);

            slaveInfo.Descendants().ToList().ForEach(current =>
            {
                ExtensibilityHelper.CreateDynamicData(extensionFactory, current);
            });

            EcHL.FreeContext(EcHL.CreateContext());

            // create EC Master
            var master = new EcMaster(slaveInfo, settings, extensionFactory, loggerFactory.CreateLogger("Master"));
            master.Configure();

            // create variable references
            var myVariable = slaveInfo.ChildSet.First().DynamicData.PdoSet.First().VariableSet.First();

            // start master
            var cts = new CancellationTokenSource();

            var task = Task.Run(() =>
            {
                var sleepTime = 1000 / (int)cycleFrequency;

                while (!cts.IsCancellationRequested)
                {
                    master.UpdateIo(DateTime.Now);

                    unsafe
                    {
                        // get Span<T> more easily?
                        var myVariableSpan = new Span<int>(myVariable.DataPtr.ToPointer(), 1);
                        myVariableSpan[0] = 83;
                    }

                    Thread.Sleep(sleepTime);
                }
            }, cts.Token);

            // wait for stop signal
            Console.WriteLine("Press any key to stop.");
            Console.ReadKey();

            cts.Cancel();

            await task;
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
