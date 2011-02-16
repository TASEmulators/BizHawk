namespace BizHawk.Emulation.Consoles.TurboGrafx
{
    public partial class PCEngine
    {
        // Street Fighter 2 was a 20-megabit HuCard. The PCE has a maximum 8-megabit addressable ROM space.
        // Therefore SF2 had a special mapper to make this work.

        // TODO: need to update SF2 mapper to reflect updates made to the primary mapper
        // (ie, the IOBuffer)
        // However, I believe more fixes will be made in the future, and SF2 works, so this is not
        // currently a priority.
        
        private byte SF2MapperLatch;

        private byte ReadMemorySF2(ushort addr)
        {
            int page = Cpu.MPR[addr >> 13];
            ushort addr13 = (ushort)(addr & 0x1FFF);

            if (page < 0x40)
            {
                // read rom.
                return RomData[(page % RomPages << 13) | (addr13)];
            }
            if (page < 0x80)
            {
                // read rom with extended SF2 mapper.
                return RomData[(((page << 13) | addr13) & 0x7FFFF) + ((SF2MapperLatch + 1)*0x80000)];
            }
            if (page >= 0xF8 && page <= 0xFB)
            {
                // read RAM.
                return Ram[((page-0xF8) << 13) | addr13];
            }

            if (page == 0xFF)
            {
                // hardware page.
                if (addr13 < 0x400) return VDC1.ReadVDC(addr13 & 0x03);
                if (addr13 < 0x800) return VCE.ReadVCE((addr13 & 0x07));
                if ((addr13 & ~1) == 0x0C00) return Cpu.TimerValue;
                if (addr13 >= 0x1000 && addr13 < 0x1400) return ReadInput();
                if (addr13 == 0x1402) return Cpu.IRQControlByte;
                if (addr13 == 0x1403) return Cpu.ReadIrqStatus();
            }
            Log.Error("MEM", "UNHANDLED READ: [{0:X2}] {1:X4}", page, addr13);
            return 0xFF;
        }
        
        private void WriteMemorySF2(ushort addr, byte value)
        {
            int page = Cpu.MPR[addr >> 13];
            ushort addr13 = (ushort)(addr & 0x1FFF);

            if ((addr & 0x1FFC) == 0x1FF0)
            {
                // Set SF2 pager.
                SF2MapperLatch = (byte) (addr & 0x03);
                return;
            }

            if (page >= 0xF8 && page <= 0xFB)
            {
                // write RAM.
                Ram[addr13] = value;
            }

            if (page == 0xFF)
            {
                // hardware page.
                if (addr13 < 0x400)
                    VDC1.WriteVDC(addr13 & 3, value);
                else if (addr13 < 0x800)
                    VCE.WriteVCE(addr13 & 7, value);
                else if (addr13 < 0x80A)
                    PSG.WritePSG(addr13, value, Cpu.TotalExecutedCycles);
                else if (addr13 == 0x0C00)
                    Cpu.WriteTimer(value);
                else if (addr13 == 0x0C01)
                    Cpu.WriteTimerEnable(value);
                else if (addr13 >= 0x1000 && addr13 < 0x1400)
                    WriteInput(value);
                else if (addr13 == 0x1402)
                    Cpu.WriteIrqControl(value);
                else if (addr13 == 0x1403)
                    Cpu.WriteIrqStatus();
            }
        }
    }
}