
namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// Pentagon 128k Memory
    /// </summary>
    public partial class Pentagon128 : SpectrumBase
    {
        /* 128k paging controlled by writes to port 0x7ffd
         * 
         * 
         
            #7FFD (32765) - decoded as A15=0, A1=0 and /IORQ=0. Bits 0..5 are latched. Bits 0..2 select RAM bank in secton D. Bit 3 selects RAM bank to dispay screen (0 - RAM5, 1 - RAM7). Bit 4 selects ROM bank (0 - ROM0, 1 - ROM1). Bit 5, when set locks future writing to #7FFD port until reset. Reading #7FFD port is the same as writing #FF into it.
            #BFFD (49149) - write data byte into AY-3-8912 chip.
            #FFFD (65533) - select AY-3-8912 addres (D4..D7 ignored) and reading data byte.

         *  0xffff +--------+--------+--------+--------+--------+--------+--------+--------+
                   | Bank 0 | Bank 1 | Bank 2 | Bank 3 | Bank 4 | Bank 5 | Bank 6 | Bank 7 |
                   |        |        |(also at|        |        |(also at|        |        |
                   |        |        | 0x8000)|        |        | 0x4000)|        |        |
                   |        |        |        |        |        | screen |        | screen |
            0xc000 +--------+--------+--------+--------+--------+--------+--------+--------+
                   | Bank 2 |        Any one of these pages may be switched in.
                   |        |
                   |        |
                   |        |
            0x8000 +--------+
                   | Bank 5 |
                   |        |
                   |        |
                   | screen |
            0x4000 +--------+--------+
                   | ROM 0  | ROM 1  | Either ROM may be switched in.
                   |        |        |
                   |        |        |
                   |        |        |
            0x0000 +--------+--------+
        */

        /// <summary>
        /// Simulates reading from the bus (no contention)
        /// Paging should be handled here
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public override byte ReadBus(ushort addr)
        {
            int divisor = addr / 0x4000;
            byte result = 0xff;

            switch (divisor)
            {
                // ROM 0x000
                case 0:
                    TestForTapeTraps(addr % 0x4000);

                    if (ROMPaged == 0)
                        result = ROM0[addr % 0x4000];
                    else
                        result = ROM1[addr % 0x4000];
                    break;

                // RAM 0x4000 (RAM5 - Bank5)
                case 1:
                    result = RAM5[addr % 0x4000];
                    break;

                // RAM 0x8000 (RAM2 - Bank2)
                case 2:
                    result = RAM2[addr % 0x4000];
                    break;

                // RAM 0xc000 (any ram bank 0 - 7 may be paged in - default bank0)
                case 3:
                    switch (RAMPaged)
                    {
                        case 0:
                            result = RAM0[addr % 0x4000];
                            break;
                        case 1:
                            result = RAM1[addr % 0x4000];
                            break;
                        case 2:
                            result = RAM2[addr % 0x4000];
                            break;
                        case 3:
                            result = RAM3[addr % 0x4000];
                            break;
                        case 4:
                            result = RAM4[addr % 0x4000];
                            break;
                        case 5:
                            result = RAM5[addr % 0x4000];
                            break;
                        case 6:
                            result = RAM6[addr % 0x4000];
                            break;
                        case 7:
                            result = RAM7[addr % 0x4000];
                            break;
                    }
                    break;
                default:
                    break;
            }

            return result;
        }

        /// <summary>
        /// Simulates writing to the bus (no contention)
        /// Paging should be handled here
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="value"></param>
        public override void WriteBus(ushort addr, byte value)
        {
            int divisor = addr / 0x4000;

            switch (divisor)
            {
                // ROM 0x000
                case 0:
                    // cannot write to ROMs
                    /*
                    if (ROMPaged == 0)
                        ROM0[addr % 0x4000] = value;
                    else
                        ROM1[addr % 0x4000] = value;
                        */
                    break;

                // RAM 0x4000 (RAM5 - Bank5 or shadow bank RAM7)
                case 1:
                    //ULADevice.RenderScreen((int)CurrentFrameCycle);
                    RAM5[addr % 0x4000] = value;
                    break;

                // RAM 0x8000 (RAM2 - Bank2)
                case 2:
                    RAM2[addr % 0x4000] = value;
                    break;

                // RAM 0xc000 (any ram bank 0 - 7 may be paged in - default bank0)
                case 3:
                    switch (RAMPaged)
                    {
                        case 0:
                            RAM0[addr % 0x4000] = value;
                            break;
                        case 1:
                            RAM1[addr % 0x4000] = value;
                            break;
                        case 2:
                            RAM2[addr % 0x4000] = value;
                            break;
                        case 3:
                            RAM3[addr % 0x4000] = value;
                            break;
                        case 4:
                            RAM4[addr % 0x4000] = value;
                            break;
                        case 5:
                            //ULADevice.RenderScreen((int)CurrentFrameCycle);
                            RAM5[addr % 0x4000] = value;
                            break;
                        case 6:
                            RAM6[addr % 0x4000] = value;
                            break;
                        case 7:
                            RAM7[addr % 0x4000] = value;
                            break;
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Reads a byte of data from a specified memory address
        /// (with memory contention if appropriate)
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public override byte ReadMemory(ushort addr)
        {
            var data = ReadBus(addr);
            return data;
        }

        /// <summary>
        /// Returns the ROM/RAM enum that relates to this particular memory read operation
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public override ZXSpectrum.CDLResult ReadCDL(ushort addr)
        {
            var result = new ZXSpectrum.CDLResult();

            int divisor = addr / 0x4000;
            result.Address = addr % 0x4000;

            switch (divisor)
            {
                // ROM 0x000
                case 0:
                    if (ROMPaged == 0)
                        result.Type = ZXSpectrum.CDLType.ROM0;
                    else
                        result.Type = ZXSpectrum.CDLType.ROM1;
                    break;

                // RAM 0x4000 (RAM5 - Bank5)
                case 1:
                    result.Type = ZXSpectrum.CDLType.RAM5;
                    break;

                // RAM 0x8000 (RAM2 - Bank2)
                case 2:
                    result.Type = ZXSpectrum.CDLType.RAM2;
                    break;

                // RAM 0xc000 (any ram bank 0 - 7 may be paged in - default bank0)
                case 3:
                    switch (RAMPaged)
                    {
                        case 0:
                            result.Type = ZXSpectrum.CDLType.RAM0;
                            break;
                        case 1:
                            result.Type = ZXSpectrum.CDLType.RAM1;
                            break;
                        case 2:
                            result.Type = ZXSpectrum.CDLType.RAM2;
                            break;
                        case 3:
                            result.Type = ZXSpectrum.CDLType.RAM3;
                            break;
                        case 4:
                            result.Type = ZXSpectrum.CDLType.RAM4;
                            break;
                        case 5:
                            result.Type = ZXSpectrum.CDLType.RAM5;
                            break;
                        case 6:
                            result.Type = ZXSpectrum.CDLType.RAM6;
                            break;
                        case 7:
                            result.Type = ZXSpectrum.CDLType.RAM7;
                            break;
                    }
                    break;
                default:
                    break;
            }

            return result;
        }

        /// <summary>
        /// Writes a byte of data to a specified memory address
        /// (with memory contention if appropriate)
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="value"></param>
        public override void WriteMemory(ushort addr, byte value)
        {
            WriteBus(addr, value);
        }

        /// <summary>
        /// Checks whether supplied address is in a potentially contended bank
        /// </summary>
        /// <param name="addr"></param>
        public override bool IsContended(ushort addr)
        {
            var a = addr & 0xc000;

            if (a == 0x4000)
            {
                // low port contention
                return true;
            }

            if (a == 0xc000)
            {
                // high port contention - check for contended bank paged in
                switch (RAMPaged)
                {
                    case 1:
                    case 3:
                    case 5:
                    case 7:
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns TRUE if there is a contended bank paged in
        /// </summary>
        /// <returns></returns>
        public override bool ContendedBankPaged()
        {
            switch (RAMPaged)
            {
                case 1:
                case 3:
                case 5:
                case 7:
                    return true;
            }

            return false;
        }

        /// <summary>
        /// ULA reads the memory at the specified address
        /// (No memory contention)
        /// Will read RAM5 (screen0) by default, unless RAM7 (screen1) is selected as output
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public override byte FetchScreenMemory(ushort addr)
        {
            byte value = new byte();

            if (SHADOWPaged && !PagingDisabled)
            {
                // shadow screen should be outputted
                // this lives in RAM7
                value = RAM7[addr & 0x3FFF];                
            }
            else
            {
                // shadow screen is not set to display or paging is disabled (probably in 48k mode) 
                // (use screen0 at RAM5)
                value = RAM5[addr & 0x3FFF];
            }

            return value;
        }

        /// <summary>
        /// Sets up the ROM
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="startAddress"></param>
        public override void InitROM(RomData romData)
        {
            RomData = romData;
            // 128k uses ROM0 and ROM1
            // 128k loader is in ROM0, and fallback 48k rom is in ROM1
            for (int i = 0; i < 0x4000; i++)
            {
                ROM0[i] = RomData.RomBytes[i];
                if (RomData.RomBytes.Length > 0x4000)
                    ROM1[i] = RomData.RomBytes[i + 0x4000];
            }
        }
    }
}
