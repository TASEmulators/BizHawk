using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
    public static class C64FormatFinder
    {
        public static C64Format GetFormat(byte[] data)
        {
            if (data == null || data.Length < 0x10)
                return C64Format.Unknown;
            using (var mem = new MemoryStream(data))
            {
                var reader = new BinaryReader(mem);
                var header = Encoding.GetEncoding(437).GetString(reader.ReadBytes(0x10));
                if (header.StartsWith("C64 CARTRIDGE   "))
                    return C64Format.CRT;
                if (header.StartsWith("GCR-1541"))
                    return C64Format.G64;
                if (header.StartsWith("C64S tape image "))
                    return C64Format.T64;
                if (header.StartsWith("C64-TAPE-RAW"))
                    return C64Format.TAP;
                if (header.StartsWith("C64File"))
                    return C64Format.P00;
                if (header.StartsWith("P64-1541"))
                    return C64Format.P64;
                if (data[0] == 0x43 && data[1] == 0x15 && data[2] == 0x41 && data[3] == 0x64)
                    return C64Format.X64;
                if (data.Length == 174848 || data.Length == 175531 || data.Length == 196608 || data.Length == 197376)
                    return C64Format.D64;
                if (data.Length == 349696 || data.Length == 351062)
                    return C64Format.D71;
                if (data.Length == 533248)
                    return C64Format.D80;
                if (data.Length == 819200 || data.Length == 822400)
                    return C64Format.D81;
                if (data.Length == 1066496)
                    return C64Format.D82;
            }
            return C64Format.Unknown;
        }
    }
}
