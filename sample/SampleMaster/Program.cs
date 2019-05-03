﻿using EtherCAT;
using EtherCAT.NET.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OneDas.Extensibility;
using System;
using System.IO;
using System.Linq;
using System.Text;
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
            var logger = loggerFactory.CreateLogger("EtherCAT Master");

            /* create EtherCAT master settings (with 10 Hz cycle frequency) */
            var cycleFrequency = 10U;
            var hardwareAddress = args.Any() ? args[0] : string.Empty;
            var esiDirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ESI");
            var settings = new EcSettings(cycleFrequency, esiDirectoryPath, hardwareAddress);

            /* create root slave info by scanning available slaves */
            var rootSlaveInfo = EcUtilities.ScanDevices(settings.NicHardwareAddress);

            rootSlaveInfo.Descendants().ToList().ForEach(current =>
            {
                ExtensibilityHelper.CreateDynamicData(settings.EsiDirectoryPath, extensionFactory, current);
            });

            /* print list of slaves */
            var message = new StringBuilder();
            var slaves = rootSlaveInfo.Descendants().ToList();

            message.AppendLine($"Found {slaves.Count()} slaves:");

            slaves.ForEach(current =>
            {
                message.AppendLine($"{current.DynamicData.Name} (PDOs: {current.DynamicData.PdoSet.Count} - CSA: { current.Csa })");
            });

            logger.LogInformation(message.ToString().TrimEnd());

            /* create variable references for later use */
            var variables = slaves.SelectMany(child => child.GetVariableSet()).ToList();

            /* create EC Master */
            using (var master = new EcMaster(settings, extensionFactory, logger))
            {
                try
                {
                    master.Configure(rootSlaveInfo);
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
                    var sleepTime = 1000 / (int)cycleFrequency;

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
