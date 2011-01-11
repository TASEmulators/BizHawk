namespace BizHawk.Emulation.Consoles.TurboGrafx
{
    public partial class PCEngine
    {
        // Much to my surprise, this silly I/O Buffer mechanic described in Charles MacDonald's doc is actually used by games.
        // As one example, Cyber Core requires emulation of the IOBuffer to boot.
        private byte IOBuffer;

        private byte ReadMemory(ushort addr)
        {
            int page = Cpu.MPR[addr >> 13];
            ushort addr13 = (ushort)(addr & 0x1FFF);

            if (page < 0x80) // read ROM
                return RomData[(page % RomPages << 13) | (addr13)];

            if (page >= 0xF8 && page <= 0xFB) // read RAM
                return Ram[addr13];

            if (page == 0xFF) // hardware page.
            {
                if (addr13 < 0x0400)           return VDC1.ReadVDC(addr13 & 0x03);
                if (addr13 < 0x0800)           { Cpu.PendingCycles--; return VCE.ReadVCE((addr13 & 0x07)); }
                if (addr13 < 0x080F)           return IOBuffer;
                if ((addr13 & ~1) == 0x0C00)   { IOBuffer = (byte) (Cpu.TimerValue | (IOBuffer & 0x80)); return IOBuffer; }
                if (addr13 >= 0x1000 && 
                    addr13 <  0x1400)          { IOBuffer = ReadInput(); return IOBuffer; }
                if ((addr13 & ~1) == 0x1400)   return IOBuffer;
                if (addr13 == 0x1402)          { IOBuffer = (byte) (Cpu.IRQControlByte  | (IOBuffer & 0xF8)); return IOBuffer; }
                if (addr13 == 0x1403)          { IOBuffer = (byte) (Cpu.ReadIrqStatus() | (IOBuffer & 0xF8)); return IOBuffer; }
            }

            Log.Error("MEM", "UNHANDLED READ: [{0:X2}] {1:X4}", page, addr13);
            return 0xFF;
        }

        private void WriteMemory(ushort addr, byte value)
        {
            int page = Cpu.MPR[addr >> 13];
            ushort addr13 = (ushort)(addr & 0x1FFF);

            if (page >= 0xF8 && page <= 0xFB) // write RAM.
            {
                if (Cpu.debug)
                    Log.Note("MEM", "*Mem* Changed {0:X4} from {1:X2} to {2:X2}", addr13, Ram[addr13], value);
                Ram[addr13] = value;
            }

            if (page == 0xFF) // hardware page.
            {
                     if (addr13 <  0x0400)     VDC1.WriteVDC(addr13 & 3, value);
                else if (addr13 <  0x0800)     { Cpu.PendingCycles--; VCE.WriteVCE(addr13 & 7, value); }
                else if (addr13 <  0x080A)     { IOBuffer = value; PSG.WritePSG(addr13, value, Cpu.TotalExecutedCycles); }
                else if (addr13 == 0x0C00)     { IOBuffer = value; Cpu.WriteTimer(value); }
                else if (addr13 == 0x0C01)     { IOBuffer = value; Cpu.WriteTimerEnable(value); }
                else if (addr13 >= 0x1000 && 
                         addr13 <  0x1400)     { IOBuffer = value; WriteInput(value); }
                else if (addr13 == 0x1402)     { IOBuffer = value; Cpu.WriteIrqControl(value); }
                else if (addr13 == 0x1403)     { IOBuffer = value; Cpu.WriteIrqStatus(); }
                else Log.Error("MEM", "unhandled hardware write [{0:X4}] : {1:X2}", addr13, value);
            }
        }
    }
}