using System;

namespace EtherCAT.NET.Extensibility
{
    [AttributeUsage(validOn: AttributeTargets.Class, AllowMultiple = true)]
    public class TargetSlaveAttribute : Attribute
    {
        public TargetSlaveAttribute(uint manufacturer, uint productCode)
        {
            this.Manufacturer = manufacturer;
            this.ProductCode = productCode;
        }

        public uint Manufacturer { get; }
        public uint ProductCode { get; }
    }
}
