using System;

namespace BizHawk.Emulation.Consoles.Sega
{
    partial class Genesis
    {
        private int BankRegion;

        public byte ReadMemoryZ80(ushort address)
        {
            if (address < 0x4000)
            {
                //Console.WriteLine("read z80 memory {0:X4}: {1:X2}",address, Z80Ram[address & 0x1FFF]);
                return Z80Ram[address & 0x1FFF];
            }
            if (address >= 0x4000 && address < 0x6000)
            {
                //Console.WriteLine(" === Z80 READS FM STATUS ===");
                return YM2612.ReadStatus();
            }
            if (address >= 0x8000)
            {
                // 68000 Bank region
                return (byte) ReadByte(BankRegion | (address & 0x7FFF));
            }
            Console.WriteLine("UNHANDLED Z80 READ {0:X4}",address);
            return 0xCD;
        }

        public void WriteMemoryZ80(ushort address, byte value)
        {
            if (address < 0x4000)
            {
                //Console.WriteLine("write z80 memory {0:X4}: {1:X2}",address, value);
                Z80Ram[address & 0x1FFF] = value;
                return;
            }
            if (address >= 0x4000 && address < 0x6000)
            {
                //Console.WriteLine(" === Z80 WRITES Z80 {0:X4}:{1:X2} ===",address, value);
                YM2612.Write(address & 3, value);
                return;
            }
            if (address == 0x6000)
            {
                BankRegion >>= 1;
                BankRegion |= (value & 1) << 23;
                BankRegion &= 0x00FF8000;
                Console.WriteLine("Bank pointing at {0:X8}",BankRegion);
                return;
            }
            if (address >= 0x8000)
            {
                WriteByte(BankRegion | (address & 0x7FFF), (sbyte) value);
                return;
            }
            Console.WriteLine("UNHANDLED Z80 WRITE {0:X4}:{1:X2}", address, value);
        }
    }
}
