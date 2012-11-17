using System;
using System.IO;

namespace BizHawk.Emulation.Consoles.Coleco
{
    public partial class ColecoVision
    {
        public byte ReadMemory(ushort addr)
        {
            if (addr >= 0x8000)
                return RomData[addr & 0x7FFF];
            if (addr >= 0x6000)
                return Ram[addr & 1023];
            if (addr < 0x2000)
                return BiosRom[addr];

            //Console.WriteLine("Unhandled read at {0:X4}", addr);
            return 0xFF;
        }

        public void WriteMemory(ushort addr, byte value)
        {
            if (addr >= 0x6000 && addr < 0x8000)
            {
                Ram[addr & 1023] = value;
                return;
            }

            //Console.WriteLine("Unhandled write at {0:X4}:{1:X2}", addr, value);
        }
    }
}
