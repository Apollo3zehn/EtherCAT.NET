using System;
using System.Net;
using System.Runtime.InteropServices;

namespace EtherCAT.NET.Infrastructure
{
    [StructLayout(LayoutKind.Sequential)]
    internal class eoe_ip4_addr_t
    {
        public uint addr;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class eoe_ethaddr_t
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] addr = new byte[6];
    }

    [StructLayout(LayoutKind.Sequential)]
    public class eoe_param_t
    {
        private byte settings = 0;
        private eoe_ethaddr_t mac = new eoe_ethaddr_t();
        private eoe_ip4_addr_t ip = new eoe_ip4_addr_t();
        private eoe_ip4_addr_t subnet = new eoe_ip4_addr_t();
        private eoe_ip4_addr_t default_gateway = new eoe_ip4_addr_t();
        private eoe_ip4_addr_t dns_ip = new eoe_ip4_addr_t();
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        private char[] dns_name = new char[32];


        public bool MacSet
        {
            get { return BitIsSet(0); }
            set { setBit(value, 0); }
        }

        public bool IpSet
        {
            get { return BitIsSet(1); }
            set { setBit(value, 1); }
        }

        public bool SubnetSet
        {
            get { return BitIsSet(2); }
            set { setBit(value, 2); }
        }

        public bool DefaultGatewaySet
        {
            get { return BitIsSet(3); }
            set { setBit(value, 3); }
        }

        public bool DNSIpSet
        {
            get { return BitIsSet(4); }
            set { setBit(value, 4); }
        }

        public bool DNSNameSet
        {
            get { return BitIsSet(5); }
            set { setBit(value, 5); }
        }

        public byte[] MAC
        {
            get { return mac.addr; }
            set { if(value.Length <= 6) value.CopyTo(mac.addr, 0); }
        }

        public string Ip
        {
            get { return UintToString(ip.addr); }
            set { ip.addr = StringToUInt(value);  }
        }

        public string Subnet
        {
            get { return UintToString(subnet.addr); }
            set { subnet.addr = StringToUInt(value); }
        }

        public string DefaultGateway
        {
            get { return UintToString(default_gateway.addr); }
            set { default_gateway.addr = StringToUInt(value); }
        }

        public string DNSIp
        {
            get { return UintToString(dns_ip.addr); }
            set { dns_ip.addr = StringToUInt(value); }
        }

        public string DNSName
        {
            get { return dns_name.ToString(); }
            set { if(value.Length <= 32) value.ToCharArray().CopyTo(dns_name, 0); }
        }



        private string UintToString(uint address)
        {
            return new IPAddress(address).ToString();
        }

        private uint StringToUInt(string address)
        { 
            byte[] byteArray = IPAddress.Parse(address).GetAddressBytes();
            return BitConverter.ToUInt32(byteArray, 0);
        }

        private bool BitIsSet(int bitPosition)
        {
            return (settings & (1 << bitPosition)) == (1 << bitPosition);
        }

        private void setBit(bool value, int bitPosition)
        {
            settings = (byte)(settings & ~(byte)(1 << bitPosition) | (byte)((value ? 1 : 0) << bitPosition));
        }
    }
}
