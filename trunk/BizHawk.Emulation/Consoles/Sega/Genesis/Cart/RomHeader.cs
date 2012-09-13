using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Sega
{
    partial class Genesis
    {
        public string RH_Console        { get { return GetRomString(0x100, 0x10); } }
        public string RH_Copyright      { get { return GetRomString(0x110, 0x10); } }
        public string RH_NameDomestic   { get { return GetRomString(0x120, 0x30); } }
        public string RH_NameExport     { get { return GetRomString(0x150, 0x30); } }
        public int    RH_RomSize        { get { return GetRomLongWord(0x1A4); } }
        public string RH_Region         { get { return GetRomString(0x1F0, 3); } }

        public bool   RH_SRamPresent    { get { return (RomData[0x1B2] & 0x40) != 0; } }
        public int    RH_SRamCode       { get { return (RomData[0x1B2] >> 3) & 3; } }
        public int    RH_SRamStart      { get { return GetRomLongWord(0x1B4); } }
        public int    RH_SRamEnd        { get { return GetRomLongWord(0x1B8); } }

        public string RH_SRamInterpretation()
        {
            switch (RH_SRamCode)
            {
                case 0: return "Even and odd addresses";
                case 2: return "Even addresses";
                case 3: return "Odd addresses";
                default: return "Invalid type";
            }
        }

        string GetRomString(int offset, int len)
        {
            return Encoding.ASCII.GetString(RomData, offset, len).Trim();
        }

        int GetRomLongWord(int offset)
        {
            return (RomData[offset] << 24) | (RomData[offset + 1] << 16) | (RomData[offset + 2] << 8) | RomData[offset + 3];
        }

        void LogCartInfo()
        {
            Console.WriteLine("==================");
            Console.WriteLine("ROM Cartridge Data");
            Console.WriteLine("==================");
            Console.WriteLine("System:     {0}", RH_Console);
            Console.WriteLine("Copyright:  {0}", RH_Copyright);
            Console.WriteLine("Name (Dom): {0}", RH_NameDomestic);
            Console.WriteLine("Name (Exp): {0}", RH_NameExport);
            Console.WriteLine("Region:     {0}", RH_Region);
            Console.WriteLine("Rom Size:   {0,7} (${0:X})", RH_RomSize);
            Console.WriteLine("SRAM Used:  {0}", RH_SRamPresent);
            if (RH_SRamPresent)
            {
                Console.WriteLine("SRAM Start: {0,7} (${0:X})", RH_SRamStart);
                Console.WriteLine("SRAM End:   {0,7} (${0:X})", RH_SRamEnd);
                Console.WriteLine("SRAM Type:  {0}", RH_SRamInterpretation());
            }
        }
    }
}
