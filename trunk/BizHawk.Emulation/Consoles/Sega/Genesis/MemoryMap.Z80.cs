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
                return YM2612.ReadStatus(SoundCPU.TotalExecutedCycles); // TODO: more than 1 read port probably?
            }
            if (address >= 0x8000)
            {
                // 68000 Bank region
                return (byte) ReadByte(BankRegion | (address & 0x7FFF));
            }
            if (address <= 0x6100) // read from bank address register - returns FF
                return 0xFF; 
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
                //Console.WriteLine(" === Z80 WRITES YM2612 {0:X4}:{1:X2} ===",address, value);
                YM2612.Write(address & 3, value, SoundCPU.TotalExecutedCycles);
                return;
            }
            if (address < 0x6100)
            {
                BankRegion >>= 1;
                BankRegion |= (value & 1) << 23;
                BankRegion &= 0x00FF8000;
                //Console.WriteLine("Bank pointing at {0:X8}",BankRegion);
                return;
            }
            if (address >= 0x7F00 && address < 0x7F20)
            {
                switch (address & 0x1F)
                {
                    case 0x00:
                    case 0x02:
                        VDP.WriteVdpData((ushort) ((value<<8) | value));
                        return;
                    
                    case 0x04:
                    case 0x06:
                        VDP.WriteVdpControl((ushort)((value << 8) | value));
                        return;

                    case 0x11:
                    case 0x13:
                    case 0x15:
                    case 0x17:
                        PSG.WritePsgData(value, SoundCPU.TotalExecutedCycles);
                        return;
                }
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
