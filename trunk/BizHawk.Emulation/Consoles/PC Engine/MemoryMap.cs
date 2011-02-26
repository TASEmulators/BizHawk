namespace BizHawk.Emulation.Consoles.TurboGrafx
{
    public partial class PCEngine
    {
        private byte IOBuffer;

        private byte ReadMemory(int addr)
        {
            if (addr < 0xFFFFF) // read ROM
                return RomData[addr % RomLength];

            if (addr >= 0x1F0000 && addr < 0x1F8000) // read RAM
                return Ram[addr & 0x1FFF];

            if (addr >= 0x1FE000) // hardware page.
            {
                if (addr < 0x1FE400)           return VDC1.ReadVDC(addr & 0x03);
                if (addr < 0x1FE800)           { Cpu.PendingCycles--; return VCE.ReadVCE((addr & 0x07)); }
                if (addr < 0x1FE80F)           return IOBuffer;
                if ((addr & ~1) == 0x1FEC00)   { IOBuffer = (byte) (Cpu.TimerValue | (IOBuffer & 0x80)); return IOBuffer; }
                if (addr >= 0x1FF000 && 
                    addr <  0x1FF400)          { IOBuffer = ReadInput(); return IOBuffer; }
                if ((addr & ~1) == 0x1FF400)   return IOBuffer;
                if (addr == 0x1FF402)          { IOBuffer = (byte) (Cpu.IRQControlByte  | (IOBuffer & 0xF8)); return IOBuffer; }
                if (addr == 0x1FF403)          { IOBuffer = (byte) (Cpu.ReadIrqStatus() | (IOBuffer & 0xF8)); return IOBuffer; }
            }

            Log.Error("MEM", "UNHANDLED READ: {0:X6}", addr);
            return 0xFF;
        }

        private void WriteMemory(int addr, byte value)
        {
            if (addr >= 0x1F0000 && addr < 0x1F8000) // write RAM.
            {
                if (Cpu.debug)
                    Log.Note("MEM", "*Mem* Changed {0:X4} from {1:X2} to {2:X2}", addr & 0x1FFF, Ram[addr & 0x1FFF], value);
                Ram[addr & 0x1FFF] = value;
            }

            else if (addr >= 0x1FE000) // hardware page.
            {
                     if (addr <  0x1FE400)     VDC1.WriteVDC(addr & 3, value);
                else if (addr <  0x1FE800)     { Cpu.PendingCycles--; VCE.WriteVCE(addr & 7, value); }
                else if (addr <  0x1FE80A)     { IOBuffer = value; PSG.WritePSG((byte)addr, value, Cpu.TotalExecutedCycles); }
                else if (addr == 0x1FEC00)     { IOBuffer = value; Cpu.WriteTimer(value); }
                else if (addr == 0x1FEC01)     { IOBuffer = value; Cpu.WriteTimerEnable(value); }
                else if (addr >= 0x1FF000 && 
                         addr <  0x1FF400)     { IOBuffer = value; WriteInput(value); }
                else if (addr == 0x1FF402)     { IOBuffer = value; Cpu.WriteIrqControl(value); }
                else if (addr == 0x1FF403)     { IOBuffer = value; Cpu.WriteIrqStatus(); }
                else Log.Error("MEM", "unhandled hardware write [{0:X6}] : {1:X2}", addr, value);
            }
            else 
                Log.Error("MEM","UNHANDLED WRITE: {0:X6}:{1:X2}",addr,value);
        }
    }
}