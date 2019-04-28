using Microsoft.DotNet.PlatformAbstractions;
using SOEM.PInvoke;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace EtherCAT.NET.Tests
{
    public class PInvokeTests
    {
        [Fact]
        public void CanAccessNativeLib()
        {
            // Arrange
            Directory.EnumerateFiles("./runtimes/", "*soem_wrapper.*", SearchOption.AllDirectories).ToList().ForEach(filePath =>
            {
                if (filePath.Contains(RuntimeEnvironment.RuntimeArchitecture))
                {
                    File.Copy(filePath, Path.GetFileName(filePath), true);
                }
            });

            // Act
            var context = EcHL.CreateContext();

            // Assert
            Assert.True(context != IntPtr.Zero);
            EcHL.FreeContext(context);
        }
    }
}