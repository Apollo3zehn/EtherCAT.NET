using System;
using System.Text.RegularExpressions;

namespace EtherCAT.NET.Infrastructure
{
    class EhterCatDataTypeHelper
    {
        public static EthercatDataType ParseEthercatDataType(string value, out int? arrayLength)
        {
            var normalizedType = NormalizeEthercatType(value, out arrayLength);

            if (Enum.TryParse<EthercatDataType>(normalizedType, ignoreCase: true, out var dataType))
                return dataType;
            else
                return EthercatDataType.Unknown;
        }

        private static string NormalizeEthercatType(string value, out int? arrayLength)
        {
            arrayLength = null;

            if (string.IsNullOrWhiteSpace(value))
                return value;

            var s = value.Trim();

            // ARRAY [lo..hi] OF <Elem>
            var ma = Regex.Match(s, @"^ARRAY\s*\[\s*(\d+)\s*\.\.\s*(\d+)\s*\]\s*OF\s*([A-Za-z_]\w*)$", RegexOptions.IgnoreCase);

            if (ma.Success)
            {
                int lo = int.Parse(ma.Groups[1].Value);
                int hi = int.Parse(ma.Groups[2].Value);
                arrayLength = hi - lo + 1;
                return ma.Groups[3].Value; // Element-Typ (z.B. BYTE, UNSIGNED8, BOOL, ...)
            }

            // STRING(n) / WSTRING(n) / OCTET STRING(n)
            if (Regex.IsMatch(s, @"^STRING\s*\(\s*\d+\s*\)$", RegexOptions.IgnoreCase)) return "VisibleString";
            if (Regex.IsMatch(s, @"^WSTRING\s*\(\s*\d+\s*\)$", RegexOptions.IgnoreCase)) return "UnicodeString";
            if (Regex.IsMatch(s, @"^OCTET\s+STRING\s*\(\s*\d+\s*\)$", RegexOptions.IgnoreCase)) return "OctetString";

            return s;
        }
    };

    public enum EthercatDataType
    {
        Unknown = 0,

        // FAL defined data types
        //      Fixed length types
        //          Boolean types
        Boolean = 1,
        BOOL = 1,                                   // <- not specified in standard, but occuring in ESI files!

        //          Bitstring types
        BIT2 = 31,
        BIT3 = 32,
        BIT4 = 33,
        BIT5 = 34,
        BIT6 = 35,
        BIT7 = 36,
        BIT8 = 37,
        BITARR8 = 45,
        BITARR16 = 46,
        BITARR32 = 47,

        //          [Currency types]

        //          Data/Time types
        TimeOfDay = 12,
        TimeDifference = 13,

        //          [Enumerated types]

        //          [Handle types]

        //          Numeric types
        @float = 8,
        Float32 = 8,
        REAL = 8,                                   // <- not specified in standard, but occuring in ESI files!

        @double = 17,
        Float64 = 17,

        Integer8 = 2,
        SINT = 2,
        @char = 2,

        Integer16 = 3,
        INT = 3,
        @short = 3,

        Integer24 = 16,

        Integer32 = 4,
        DINT = 4,
        @long = 4,

        Integer40 = 18,
        Integer48 = 19,
        Integer56 = 20,

        Integer64 = 21,
        LINT = 21,

        Unsigned8 = 5,
        USINT = 5,
        unsigned_char = 5,
        BYTE = 5,

        Unsigned16 = 6,
        UINT = 6,
        WORD = 6,

        Unsigned24 = 22,

        Unsigned32 = 7,
        UDINT = 7,

        Unsigned40 = 24,
        Unsigned48 = 25,
        Unsigned56 = 26,

        Unsigned64 = 27,
        ULINT = 27,

        //          [Pointer types]
        //          [OctetString types]
        //          [VisibleString character types]

        //      String types
        OctetString = 10,
        VisibleString = 9,
        UnicodeString = 11,


        //      GUID Types
        GUID = 29
    }
}
