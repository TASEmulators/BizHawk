namespace BizHawk.Emulation.Consoles.TurboGrafx
{
    public partial class PCEngine
    {
        // The SuperGrafx has 32K of RAM and a different port configuration to allow
        // I/O access to VDC1, VDC2, and the VPC.

        private byte ReadMemorySGX(ushort addr)
        {
            int page = Cpu.MPR[addr >> 13];
            ushort addr13 = (ushort)(addr & 0x1FFF);

            if (page < 0x80) // read ROM
                return RomData[(page % RomPages << 13) | (addr13)];

            if (page >= 0xF8 && page <= 0xFB) // read RAM
                return Ram[((page-0xF8) << 13) | addr13];

            if (page == 0xFF) // hardware page.
            {
                if (addr13 < 0x400)
                {
                    addr13 &= 0x1F;
                    if (addr13 <= 0x07) return VDC1.ReadVDC(addr13 & 3);
                    if (addr13 <= 0x0F) return VPC.ReadVPC(addr13);
                    if (addr13 <= 0x17) return VDC2.ReadVDC(addr13 & 3);
                                        return 0xFF;
                }
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

        private void WriteMemorySGX(ushort addr, byte value)
        {
            int page = Cpu.MPR[addr >> 13];
            ushort addr13 = (ushort)(addr & 0x1FFF);

            if (page >= 0xF8 && page <= 0xFB) // write RAM.
                Ram[((page-0xF8) << 13) | addr13] = value;

            else if (page == 0xFF) // hardware page.
            {
                if (addr13 <  0x0400)     
                {
                    addr13 &= 0x1F;
                         if (addr13 <= 0x07) VDC1.WriteVDC(addr13 & 3, value);
                    else if (addr13 <= 0x0F) VPC.WriteVPC(addr13, value);
                    else if (addr13 <= 0x17) VDC2.WriteVDC(addr13 & 3, value);
                }
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