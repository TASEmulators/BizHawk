namespace BizHawk.Emulation.Consoles.TurboGrafx
{
    public partial class PCEngine
    {
        // Street Fighter 2 was a 20-megabit HuCard. The PCE has a maximum 8-megabit addressable ROM space.
        // Therefore SF2 had a special mapper to make this work.
        
        byte SF2MapperLatch;

        byte ReadMemorySF2(int addr)
        {
            if (addr < 0x7FFFF) // read ROM
                return RomData[addr];

            if (addr < 0xFFFFF) // read ROM
                return RomData[(addr & 0x7FFFF) + ((SF2MapperLatch + 1) * 0x80000)];

            if (addr >= 0x1F0000 && addr < 0x1F8000) // read RAM
                return Ram[addr & 0x1FFF];

            if (addr >= 0x1FE000) // hardware page.
            {
                if (addr < 0x1FE400)           return VDC1.ReadVDC(addr);
                if (addr < 0x1FE800)           { Cpu.PendingCycles--; return VCE.ReadVCE(addr); }
                if (addr < 0x1FEC00)           return IOBuffer;
                if (addr < 0x1FF000)           { IOBuffer = (byte) (Cpu.ReadTimerValue() | (IOBuffer & 0x80)); return IOBuffer; }
                if (addr >= 0x1FF000 && 
                    addr <  0x1FF400)          { IOBuffer = ReadInput(); return IOBuffer; }
                if ((addr & ~1) == 0x1FF400)   return IOBuffer;
                if (addr == 0x1FF402)          { IOBuffer = Cpu.IRQControlByte; return IOBuffer; }
                if (addr == 0x1FF403)          { IOBuffer = (byte) (Cpu.ReadIrqStatus() | (IOBuffer & 0xF8)); return IOBuffer; }
            }

            Log.Error("MEM", "UNHANDLED READ: {0:X6}", addr);
            return 0xFF;
        }
        
        void WriteMemorySF2(int addr, byte value)
        {
            if ((addr & 0x1FFC) == 0x1FF0)
            {
                // Set SF2 pager.
                SF2MapperLatch = (byte)(addr & 0x03);
                return;
            }

            if (addr >= 0x1F0000 && addr < 0x1F8000) // write RAM.
                Ram[addr & 0x1FFF] = value;

            else if (addr >= 0x1FE000) // hardware page.
            {
                     if (addr < 0x1FE400)    VDC1.WriteVDC(addr, value);
                else if (addr < 0x1FE800)    { Cpu.PendingCycles--; VCE.WriteVCE(addr, value); }
                else if (addr < 0x1FEC00)    { IOBuffer = value; PSG.WritePSG((byte)addr, value, Cpu.TotalExecutedCycles); }
                else if (addr == 0x1FEC00)   { IOBuffer = value; Cpu.WriteTimer(value); }
                else if (addr == 0x1FEC01)   { IOBuffer = value; Cpu.WriteTimerEnable(value); }
                else if (addr >= 0x1FF000 &&
                         addr < 0x1FF400)    { IOBuffer = value; WriteInput(value); }
                else if (addr == 0x1FF402)   { IOBuffer = value; Cpu.WriteIrqControl(value); }
                else if (addr == 0x1FF403)   { IOBuffer = value; Cpu.WriteIrqStatus(); }
                else Log.Error("MEM", "unhandled hardware write [{0:X6}] : {1:X2}", addr, value);
            }
            else
                Log.Error("MEM", "UNHANDLED WRITE: {0:X6}:{1:X2}", addr, value);
        }
    }
}