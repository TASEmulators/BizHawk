using System;
using System.IO;
using System.Globalization;

namespace BizHawk.Emulation.Consoles.Sega
{
    public sealed partial class GenVDP : IVideoProvider
    {
        // Memory
        public byte[]   VRAM      = new byte[0x10000];
        public ushort[] CRAM      = new ushort[64];
        public ushort[] VSRAM     = new ushort[40];
        public byte[]   Registers = new byte[0x20];
        
        public byte[]   PatternBuffer = new byte[0x20000];
        public int[]    Palette       = new int[64];
        public int[]    FrameBuffer   = new int[320*224];
        public int      FrameWidth    = 320;
        public int      FrameHeight   = 224;

        public int      ScanLine;
        public int      HIntLineCounter;

        public bool     HInterruptsEnabled   { get { return (Registers[0]  & 0x10) != 0; } }
        public bool     DisplayEnabled       { get { return (Registers[1]  & 0x40) != 0; } }
        public bool     VInterruptEnabled    { get { return (Registers[1]  & 0x20) != 0; } }
        public bool     DmaEnabled           { get { return (Registers[1]  & 0x10) != 0; } }
        public bool     CellBasedVertScroll  { get { return (Registers[11] & 0x08) != 0; } }

        public bool     InDisplayPeriod      { get { return ScanLine < 224 && DisplayEnabled; } }

        ushort NameTableAddrA;
        ushort NameTableAddrB;
        ushort NameTableAddrWindow;
        ushort SpriteAttributeTableAddr;
        ushort HScrollTableAddr;
        int    NameTableWidth = 32;
        int    NameTableHeight = 32;

        int    DisplayWidth;
        int    SpriteLimit;
        int    SpritePerLineLimit;
        int    DotsPerLineLimit;

        bool   ControlWordPending;
        ushort VdpDataAddr;
        byte   VdpDataCode;
        
        const int CommandVramRead   = 0;
        const int CommandVramWrite  = 1;
        const int CommandCramWrite  = 3;
        const int CommandVsramRead  = 4;
        const int CommandVsramWrite = 5;
        const int CommandCramRead   = 8;

        public ushort VdpStatusWord = 0x3400;
        public const int StatusHorizBlanking            = 0x04;
        public const int StatusVerticalBlanking         = 0x08;
        public const int StatusOddFrame                 = 0x10;
        public const int StatusSpriteCollision          = 0x20;
        public const int StatusSpriteOverflow           = 0x40;
        public const int StatusVerticalInterruptPending = 0x80;

        public bool VdpDebug = false;

        public Func<int> GetPC;

        public GenVDP()
        {
            WriteVdpRegister(00, 0x04);
            WriteVdpRegister(01, 0x04);
            WriteVdpRegister(02, 0x30);
            WriteVdpRegister(03, 0x3C);
            WriteVdpRegister(04, 0x07);
            WriteVdpRegister(05, 0x67);
            WriteVdpRegister(10, 0xFF);
            WriteVdpRegister(12, 0x81);
            WriteVdpRegister(15, 0x02);
            Log.Note("VDP", "VDP init routine complete");
        }

        public ushort ReadVdp(int addr)
        {
            switch (addr)
            {
                case 0:
                case 2:
                    return ReadVdpData();
                case 4:
                case 6:
                    return ReadVdpControl();
                default:
                    return ReadHVCounter();
            }
        }

        public void WriteVdp(int addr, ushort data)
        {
            switch (addr)
            {
                case 0:
                case 2:
                    WriteVdpData(data);
                    return;
                case 4:
                case 6:
                    WriteVdpControl(data);
                    return;
            }
        }

        public void WriteVdpControl(ushort data)
        {
            Log.Note("VDP", "Control Write {0:X4} (PC={1:X6})", data, GetPC());

            if (ControlWordPending == false)
            {
                if ((data & 0xC000) == 0x8000)
                {
                    int reg = (data >> 8) & 0x1F;
                    byte value = (byte) (data & 0xFF);
                    WriteVdpRegister(reg, value);
                    VdpDataCode = 0;
                } else {
                    ControlWordPending = true;
                    VdpDataAddr &= 0xC000;
                    VdpDataAddr |= (ushort) (data & 0x3FFF);
                    VdpDataCode &= 0x3C;
                    VdpDataCode |= (byte) (data >> 14);
                    //Console.WriteLine("Address = {0:X4}", VdpDataAddr);
                    //Console.WriteLine("Code = {0:X2}", VdpDataCode);
                }
            } else {
                ControlWordPending = false;

                // Update data address and code
                VdpDataAddr &= 0x3FFF;
                VdpDataAddr |= (ushort) ((data & 0x03) << 14);
                //Console.WriteLine("Address = {0:X4}", VdpDataAddr);
                VdpDataCode &= 0x03;
                VdpDataCode |= (byte) ((data >> 2) & 0x3C);
                //Log.Note("VDP", "Code = {0:X2}", VdpDataCode);

                if ((VdpDataCode & 0x20) != 0 && DmaEnabled) // DMA triggered
                {
                    //Console.WriteLine("DMA TIME!");

                    // what type of DMA?
                    switch (Registers[23] >> 6)
                    {
                        case 2:
                            Log.Note("VDP", "VRAM FILL");
                            DmaFillModePending = true;
                            break;
                        case 3:
                            Log.Error("VDP", "VRAM COPY *");
                            ExecuteVramVramCopy();
                            break;
                        default:
                            Execute68000VramCopy();
                            break;
                    }
                }
            }
        }

        public ushort ReadVdpControl()
        {
            VdpStatusWord |= 0x0200; // Fifo empty // TODO kill this, emulating the damn FIFO.
            ControlWordPending = false; // Hmm.. if this happens in an interrupt between 1st and 2nd word..

            // sprite overflow flag should clear.
            // sprite collision flag should clear.

            return VdpStatusWord;
        }

        public void WriteVdpData(ushort data)
        {
            Log.Note("VDP", "Data port write: {0:X4} (PC={1:X6})", data, GetPC());
            ControlWordPending = false; 

            // byte-swap incoming data when A0 is set
             if ((VdpDataAddr & 1) != 0)
            {
                data = (ushort)((data >> 8) | (data << 8));
                Log.Error("VDP", "VRAM byte-swap is happening because A0 is not 0. [{0:X4}] = {1:X4}", VdpDataAddr, data);
            }

            switch (VdpDataCode & 0xF)
            {
                case CommandVramWrite: // VRAM Write
                    VRAM[VdpDataAddr & 0xFFFE] = (byte) data;
                    VRAM[(VdpDataAddr & 0xFFFE) + 1] = (byte) (data >> 8);
                    //if (VdpDebug)
                    Log.Note("VDP", "VRAM[{0:X4}] = {1:X4}", VdpDataAddr, data);
                    UpdatePatternBuffer(VdpDataAddr & 0xFFFE);
                    UpdatePatternBuffer((VdpDataAddr & 0xFFFE) + 1);
                    VdpDataAddr += Registers[0x0F];
                    break;
                case CommandCramWrite: // CRAM write
                    CRAM[(VdpDataAddr / 2) % 64] = data;
                    //if (VdpDebug)
                    Log.Note("VDP", "CRAM[{0:X2}] = {1:X4}", (VdpDataAddr / 2) % 64, data);
                    ProcessPalette((VdpDataAddr/2)%64);
                    VdpDataAddr += Registers[0x0F];
                    break;
                case CommandVsramWrite: // VSRAM write
                    VSRAM[(VdpDataAddr / 2) % 40] = data;
                    //if (VdpDebug)
                    Log.Note("VDP", "VSRAM[{0:X2}] = {1:X4}", (VdpDataAddr / 2) % 40, data);
                    VdpDataAddr += Registers[0x0F];
                    break;
                default:
                    Log.Error("VPD","VDP DATA WRITE WITH UNHANDLED CODE!!! {0}", VdpDataCode & 7);
                    break;
            }

            if (DmaFillModePending)
            {
                ExecuteVramFill(data);
            }
        }

        public ushort ReadVdpData()
        {
int orig_addr = VdpDataAddr;
            ushort retval = 0xBEEF;
            switch (VdpDataCode & 0x0F)
            {
                case CommandVramRead:
//if ((VdpDataAddr & 1) != 0) throw new Exception("VRAM read is not word-aligned. what do?");
                    retval = VRAM[VdpDataAddr & 0xFFFE];
                    retval |= (ushort) (VRAM[(VdpDataAddr & 0xFFFE) + 1] << 8);
                    VdpDataAddr += Registers[0x0F];
                    break;
                case CommandVsramRead:
                    retval = VSRAM[(VdpDataAddr / 2) % 40];
                    VdpDataAddr += Registers[0x0F];
                    return retval;
                case CommandCramRead:
                    retval = CRAM[(VdpDataAddr / 2) % 64];
                    VdpDataAddr += Registers[0x0F];
                    return retval;
                default:
                    throw new Exception("VRAM read with unexpected code!!! " + (VdpDataCode & 0x0F));
            }

            Log.Note("VDP","VDP Data Read from {0:X4} returning {1:X4}", orig_addr, retval);
            return retval;
        }

        ushort ReadHVCounter()
        {
            int vcounter = ScanLine;
            if (vcounter > 0xEA)
                vcounter -= 7; 
            // TODO generalize this across multiple video modes and stuff.

            // TODO dont tie this to musashi cycle count.
            // Figure out a "clean" way to get cycle counter information available to VDP.
            // Oh screw that. The VDP and the cpu cycle counters are going to be intertwined pretty tightly.
            int hcounter = (488 - Native68000.Musashi.GetCyclesRemaining()) * 255 / 488;
            // FIXME: totally utterly wrong.

            ushort res = (ushort) ((vcounter << 8) | (hcounter & 0xFF));
            //Console.WriteLine("READ HVC: V={0:X2} H={1:X2}  ret={2:X4}", vcounter, hcounter, res);

            return res;
        }

        public void WriteVdpRegister(int register, byte data)
        {
            //if (VdpDebug)
            Log.Note("VDP", "Register {0}: {1:X2}", register, data);
            switch (register)
            {
                case 0x00: // Mode Set Register 1
                    Registers[register] = data;
                    //if (VdpDebug)
                    //Log.Note("VDP", "HINT enabled: " + HInterruptsEnabled);
                    break;

                case 0x01: // Mode Set Register 2
                    //if (VdpDebug)
                    //{
                    //    Registers[register] = data;
                    //    Log.Note("VDP", "DisplayEnabled: " + DisplayEnabled);
                    //    Log.Note("VDP", "DmaEnabled: " + DmaEnabled);
                    //    Log.Note("VDP", "VINT enabled: " + VInterruptEnabled);
                    //}
                    break;

                case 0x02: // Name Table Address for Layer A
                    NameTableAddrA = (ushort) ((data & 0x38) << 10);
                    //if (VdpDebug)
                    //Log.Note("VDP", "SET NTa A = {0:X4}", NameTableAddrA);
                    break;

                case 0x03: // Name Table Address for Window
                    NameTableAddrWindow = (ushort) ((data & 0x3E) << 10);
                    //if (VdpDebug)
                    //Log.Note("VDP", "SET NTa W = {0:X4}", NameTableAddrWindow);
                    break;

                case 0x04: // Name Table Address for Layer B
                    NameTableAddrB = (ushort) (data << 13);
                    //if (VdpDebug)
                    //Log.Note("VDP", "SET NTa B = {0:X4}", NameTableAddrB);
                    break;

                case 0x05: // Sprite Attribute Table Address
                    SpriteAttributeTableAddr = (ushort) (data << 9);
                    //if (VdpDebug)
                    //Log.Note("VDP", "SET SAT attr = {0:X4}", SpriteAttributeTableAddr);
                    break;

                case 0x0A: // H Interrupt Register
                    //if (VdpDebug)
                    //Log.Note("VDP", "HInt occurs every {0} lines.", data);
                    break;

                case 0x0B: // VScroll/HScroll modes
                    //if (VdpDebug)
                    //{
                    //    if ((data & 4) != 0)
                    //        Log.Note("VDP", "VSCroll Every 2 Cells Enabled");
                    //    else
                    //        Log.Note("VDP", "Full Screen VScroll");

                    //    int hscrollmode = data & 3;
                    //    switch (hscrollmode)
                    //    {
                    //        case 0: Log.Note("VDP", "Full Screen HScroll"); break;
                    //        case 1: Log.Note("VDP", "Prohibited HSCROLL mode!!!  But it'll work."); break;
                    //        case 2: Log.Note("VDP", "HScroll every 1 cell"); break;
                    //        case 3: Log.Note("VDP", "HScroll every line"); break;
                    //    }
                    //}
                    break;

                case 0x0C: // Mode Set #4
                    // TODO interlaced modes
                    if ((data & 0x81) == 0)
                    {
                        // Display is 32 cells wide
                        if (DisplayWidth != 32)
                        {
                            FrameBuffer = new int[256*224];
                            FrameWidth = 256;
                            DisplayWidth = 32;
                            SpriteLimit = 64;
                            SpritePerLineLimit = 16;
                            DotsPerLineLimit = 256;
                        }
                    } else {
                        // Display is 40 cells wide
                        if (DisplayWidth != 40)
                        {
                            FrameBuffer = new int[320*224];
                            FrameWidth = 320;
                            DisplayWidth = 40;
                            SpriteLimit = 80;
                            SpritePerLineLimit = 20;
                            DotsPerLineLimit = 320;
                        }
                    }
                    break;

                case 0x0D: // H Scroll Table Address
                    HScrollTableAddr = (ushort) (data << 10);
                    //if (VdpDebug)
                    //Log.Note("VDP", "SET HScrollTab attr = {0:X4}", HScrollTableAddr);
                    break;

                case 0x0F: // Auto Address Register Increment
                    //if (VdpDebug)
                    //Log.Note("VDP", "Set Data Increment to " + data);
                    break;

                case 0x10: // Nametable Dimensions
                    switch (data & 0x03)
                    {
                        case 0: NameTableWidth = 32; break;
                        case 1: NameTableWidth = 64; break;
                        case 2: NameTableWidth = 32; break; // invalid setting
                        case 3: NameTableWidth = 128; break;
                    }
                    switch ((data>>4) & 0x03)
                    {
                        case 0: NameTableHeight = 32; break;
                        case 1: NameTableHeight = 64; break;
                        case 2: NameTableHeight = 32; break; // invalid setting
                        case 3: NameTableHeight = 128; break;
                    }
                    break;

                case 0x11: // Window H Position
                    int whp = data & 31;
                    bool fromright = (data & 0x80) != 0;
                    //if (VdpDebug)
                    //Log.Note("VDP", "Window H is {0} units from {1}", whp, fromright ? "right" : "left");
                    break;

                case 0x12: // Window V
                    whp = data & 31;
                    fromright = (data & 0x80) != 0;
                    //if (VdpDebug)
                    //Log.Note("VDP", "Window V is {0} units from {1}", whp, fromright ? "lower" : "upper");
                    break;

                case 0x13: // DMA Length Low
                    Registers[register] = data;
                    //Log.Note("VDP", "DMA Length = {0:X4}", DmaLength);
                    break;

                case 0x14: // DMA Length High
                    Registers[register] = data;
                    //Log.Note("VDP", "DMA Length = {0:X4}", DmaLength);
                    break;

                case 0x15: // DMA Source Low
                    Registers[register] = data;
                    //Log.Note("VDP", "DMA Source = {0:X6}", DmaSource);
                    break;
                case 0x16: // DMA Source Mid
                    Registers[register] = data;
                    //Log.Note("VDP", "DMA Source = {0:X6}", DmaSource);
                    break;
                case 0x17: // DMA Source High
                    Registers[register] = data;
                    //Log.Note("VDP", "DMA Source = {0:X6}", DmaSource);
                    break;

            }
            Registers[register] = data;
        }

        void ProcessPalette(int slot)
        {
            byte r = PalXlatTable[(CRAM[slot] & 0x000F) >> 0];
            byte g = PalXlatTable[(CRAM[slot] & 0x00F0) >> 4];
            byte b = PalXlatTable[(CRAM[slot] & 0x0F00) >> 8];
            Palette[slot] = Colors.ARGB(r, g, b);
        }

        void UpdatePatternBuffer(int addr)
        {
            PatternBuffer[(addr*2) + 1] = (byte) (VRAM[addr^1] & 0x0F);
            PatternBuffer[(addr*2) + 0] = (byte) (VRAM[addr^1] >> 4);
        }

        public int[] GetVideoBuffer()
        {
            return FrameBuffer;
        }

        public int VirtualWidth { get { return 320; } }

        public int BufferWidth
        {
            get { return FrameWidth; }
        }

        public int BufferHeight
        {
            get { return FrameHeight; }
        }

        public int BackgroundColor
        {
            get { return Palette[Registers[7] & 0x3F]; }
        }

        #region State Save/Load Code

        public void SaveStateText(TextWriter writer)
        {
            writer.WriteLine("[VDP]");

            writer.Write("VRAM ");
            VRAM.SaveAsHex(writer);
            writer.Write("CRAM ");
            CRAM.SaveAsHex(writer);
            writer.Write("VSRAM ");
            VSRAM.SaveAsHex(writer);
            writer.Write("Registers ");
            Registers.SaveAsHex(writer);

            writer.WriteLine("ControlWordPending {0}", ControlWordPending);
            writer.WriteLine("DmaFillModePending {0}", DmaFillModePending);
            writer.WriteLine("VdpDataAddr {0:X4}", VdpDataAddr);
            writer.WriteLine("VdpDataCode {0}", VdpDataCode);

            writer.WriteLine("[/VDP]");
        }

        public void LoadStateText(TextReader reader)
        {
            while (true)
            {
                string[] args = reader.ReadLine().Split(' ');
                if (args[0].Trim() == "") continue;
                if (args[0] == "[/VDP]") break;
                else if (args[0] == "VRAM")                 VRAM.ReadFromHex(args[1]);
                else if (args[0] == "CRAM")                 CRAM.ReadFromHex(args[1]);
                else if (args[0] == "VSRAM")                VSRAM.ReadFromHex(args[1]);
                else if (args[0] == "Registers")            Registers.ReadFromHex(args[1]);
                else if (args[0] == "ControlWordPending")   ControlWordPending = bool.Parse(args[1]);
                else if (args[0] == "DmaFillModePending")   DmaFillModePending = bool.Parse(args[1]);
                else if (args[0] == "VdpDataAddr")          VdpDataAddr = ushort.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "VdpDataCode")          VdpDataCode = byte.Parse(args[1]);
                else
                    Console.WriteLine("Skipping unrecognized identifier " + args[0]);
            }

            for (int i = 0; i < CRAM.Length; i++)
                ProcessPalette(i);
            for (int i = 0; i < VRAM.Length; i++)
                UpdatePatternBuffer(i);
            for (int i = 0; i < Registers.Length; i++)
                WriteVdpRegister(i, Registers[i]);
        }

        public void SaveStateBinary(BinaryWriter writer)
        {
            writer.Write(VRAM);
            writer.Write(CRAM);
            writer.Write(VSRAM);
            writer.Write(Registers);

            writer.Write(ControlWordPending);
            writer.Write(DmaFillModePending);
            writer.Write(VdpDataAddr);
            writer.Write(VdpDataCode);
        }

        public void LoadStateBinary(BinaryReader reader)
        {
            VRAM = reader.ReadBytes(VRAM.Length);
            CRAM = reader.ReadUInt16s(CRAM.Length);
            VSRAM = reader.ReadUInt16s(VSRAM.Length);
            Registers = reader.ReadBytes(Registers.Length);

            ControlWordPending = reader.ReadBoolean();
            DmaFillModePending = reader.ReadBoolean();
            VdpDataAddr = reader.ReadUInt16();
            VdpDataCode = reader.ReadByte();

            for (int i = 0; i < CRAM.Length; i++)
                ProcessPalette(i);
            for (int i = 0; i < VRAM.Length; i++)
                UpdatePatternBuffer(i);
            for (int i = 0; i < Registers.Length; i++)
                WriteVdpRegister(i, Registers[i]);
        }

        #endregion
    }
}