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
                if (addr < 0x1FE400)           return VDC1.ReadVDC(addr);
                if (addr < 0x1FE800)           { Cpu.PendingCycles--; return VCE.ReadVCE(addr); }
                if (addr < 0x1FEC00)           return IOBuffer;
                if (addr < 0x1FF000)           { IOBuffer = (byte) (Cpu.ReadTimerValue() | (IOBuffer & 0x80)); return IOBuffer; }
                if (addr >= 0x1FF000 && 
                    addr <  0x1FF400)          { IOBuffer = ReadInput(); return IOBuffer; }
                if ((addr & ~1) == 0x1FF400)   return IOBuffer;
                if (addr == 0x1FF402)          { IOBuffer = (byte) (Cpu.IRQControlByte  | (IOBuffer & 0xF8)); return IOBuffer; }
                if (addr == 0x1FF403)          { IOBuffer = (byte) (Cpu.ReadIrqStatus() | (IOBuffer & 0xF8)); return IOBuffer; }
                if (addr >= 0x1FF800)          return ReadCD(addr);
            }

            if (addr >= 0x1EE000 && addr <= 0x1EE7FF)   // BRAM
            {
                if (BramEnabled && BramLocked == false)
                {
                    System.Console.WriteLine("READ BRAM[{0}] ; ret {1:X2}", addr & 0x7FF, BRAM[addr & 0x7FF]);
                    return BRAM[addr & 0x7FF];
                }
                System.Console.WriteLine("attemped BRAM read while locked");
                return 0xFF;
            }

            Log.Error("MEM", "UNHANDLED READ: {0:X6}", addr);
            return 0xFF;
        }

        private void WriteMemory(int addr, byte value)
        {
            if (addr >= 0x1F0000 && addr < 0x1F8000) // write RAM.
            {
                //if (Cpu.debug)
                    //Log.Note("MEM", "*Mem* Changed {0:X4} from {1:X2} to {2:X2}", addr & 0x1FFF, Ram[addr & 0x1FFF], value);
                Ram[addr & 0x1FFF] = value;
            }

            else if (addr >= 0x1FE000) // hardware page.
            {
                     if (addr <  0x1FE400)     VDC1.WriteVDC(addr, value);
                else if (addr <  0x1FE800)     { Cpu.PendingCycles--; VCE.WriteVCE(addr, value); }
                else if (addr <  0x1FEC00)     { IOBuffer = value; PSG.WritePSG((byte)addr, value, Cpu.TotalExecutedCycles); }
                else if (addr == 0x1FEC00)     { IOBuffer = value; Cpu.WriteTimer(value); }
                else if (addr == 0x1FEC01)     { IOBuffer = value; Cpu.WriteTimerEnable(value); }
                else if (addr >= 0x1FF000 && 
                         addr <  0x1FF400)     { IOBuffer = value; WriteInput(value); }
                else if (addr == 0x1FF402)     { IOBuffer = value; Cpu.WriteIrqControl(value); }
                else if (addr == 0x1FF403)     { IOBuffer = value; Cpu.WriteIrqStatus(); }
                else if (addr >= 0x1FF800)     { WriteCD(addr, value); }
                else Log.Error("MEM", "unhandled hardware write [{0:X6}] : {1:X2}", addr, value);
            }

            else if (addr >= 0x1EE000 && addr <= 0x1EE7FF)   // BRAM
            {
                if (BramEnabled && BramLocked == false)
                {
                    System.Console.WriteLine("WRITE BRAM[{0}] : {1:X2}", addr & 0x7FF, value);
                    BRAM[addr & 0x7FF] = value;
                    SaveRamModified = true;
                }
                else System.Console.WriteLine("attemped BRAM write while locked!");
            }

            else 
                Log.Error("MEM","UNHANDLED WRITE: {0:X6}:{1:X2}",addr,value);
        }
    }
}