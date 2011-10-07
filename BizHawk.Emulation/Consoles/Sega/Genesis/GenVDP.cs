using System;

namespace BizHawk.Emulation.Consoles.Sega
{
    public sealed partial class GenVDP : IVideoProvider
    {
        // Memory
        public byte[] VRAM = new byte[0x10000];
        public ushort[] CRAM = new ushort[64];
        public ushort[] VSRAM = new ushort[40];
        public byte[] Registers = new byte[0x20];
        
        public byte[] PatternBuffer = new byte[0x20000];
        public int[] Palette = new int[64];
        public int[] FrameBuffer = new int[256*224];
        public int FrameWidth = 256;

        public int ScanLine;

        public bool HInterruptsEnabled   { get { return (Registers[0] & 0x10) != 0; } }
        public bool DisplayEnabled       { get { return (Registers[1] & 0x40) != 0; } }
        public bool VInterruptEnabled    { get { return (Registers[1] & 0x20) != 0; } }
        public bool DmaEnabled           { get { return (Registers[1] & 0x10) != 0; } }
        public bool CellBasedVertScroll  { get { return (Registers[11] & 0x08) != 0; } }
        public bool Display40Mode        { get { return (Registers[12] & 0x81) != 0; } }

        private ushort NameTableAddrA;
        private ushort NameTableAddrB;
        private ushort NameTableAddrWindow;
        private ushort SpriteAttributeTableAddr;
        private ushort HScrollTableAddr;
        private byte NameTableWidth;
        private byte NameTableHeight;

        private bool ControlWordPending;
        private ushort VdpDataAddr;
        private byte VdpDataCode;

        private static readonly byte[] PalXlatTable = { 0, 0, 36, 36, 73, 73, 109, 109, 145, 145, 182, 182, 219, 219, 255, 255 };

        public void WriteVdpControl(ushort data)
        {
            //Console.WriteLine("[PC = {0:X6}] VDP: Control Write {1:X4}", /*Genesis._MainCPU.PC*/0, data);

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
                //Console.WriteLine("Code = {0:X2}", VdpDataCode);

                if ((VdpDataCode & 0x20) != 0 && DmaEnabled) // DMA triggered
                {
                    //Console.WriteLine("DMA TIME!");

                    // what type of DMA?
                    switch (Registers[23] >> 6)
                    {
                        case 2: 
                            Console.WriteLine("VRAM FILL");
                            DmaFillModePending = true;
                            break;
                        case 3: 
                            Console.WriteLine("VRAM COPY    **** UNIMPLEMENTED ***"); 
                            break;
                        default:
                            Console.WriteLine("68k->VRAM COPY    **** UNIMPLEMENTED ***");
                            Execute68000VramCopy();
                            break;
                    }
                    Console.WriteLine("DMA LEN = "+DmaLength);
                }
            }
        }

        public ushort ReadVdpControl()
        {
            //Console.WriteLine("VDP: Control Read");
            ushort value = 0x3400; // fixed bits per genvdp.txt TODO test on everdrive, I guess.
            value |= 0x0200; // Fifo empty
            return value;
        }

        public void WriteVdpData(ushort data)
        {
            ControlWordPending = false; 

            // byte-swap incoming data when A0 is set
            if ((VdpDataAddr & 1) != 0) 
                data = (ushort) ((data >> 8) | (data << 8));

            if (DmaFillModePending)
            {
                ExecuteDmaFill(data);
            }

            switch (VdpDataCode & 7)
            {
                case 1: // VRAM Write
                    VRAM[VdpDataAddr & 0xFFFE] = (byte) data;
                    VRAM[(VdpDataAddr & 0xFFFE) + 1] = (byte) (data >> 8);
                    UpdatePatternBuffer(VdpDataAddr & 0xFFFE);
                    UpdatePatternBuffer((VdpDataAddr & 0xFFFE) + 1);
                    //Console.WriteLine("Wrote VRAM[{0:X4}] = {1:X4}", VdpDataAddr, data);
                    VdpDataAddr += Registers[0x0F];
                    break;
                case 3: // CRAM write
                    CRAM[(VdpDataAddr / 2) % 64] = data;
                    //Console.WriteLine("Wrote CRAM[{0:X2}] = {1:X4}", (VdpDataAddr / 2) % 64, data);
                    ProcessPalette((VdpDataAddr/2)%64);
                    VdpDataAddr += Registers[0x0F];
                    break;
                case 5: // VSRAM write
                    VSRAM[(VdpDataAddr / 2) % 40] = data;
                    //Console.WriteLine("Wrote VSRAM[{0:X2}] = {1:X4}", (VdpDataAddr / 2) % 40, data);
                    VdpDataAddr += Registers[0x0F];
                    break;
                default: 
                    Console.WriteLine("VDP DATA WRITE WITH UNHANDLED CODE!!!");
                    break;
            }
        }

        public ushort ReadVdpData()
        {
            //Console.WriteLine("VDP: Data Read");
            return 0;
        }

        public void WriteVdpRegister(int register, byte data)
        {
            //Console.WriteLine("Register {0}: {1:X2}", register, data);
            switch (register)
            {
                case 0x00:
                    Registers[register] = data;
                    Console.WriteLine("HINT enabled: "+ HInterruptsEnabled);
                    break;
                case 0x01:
                    Registers[register] = data;
                    Console.WriteLine("DmaEnabled: "+DmaEnabled);
                    Console.WriteLine("VINT enabled: " + VInterruptEnabled);
                    break;
                case 0x02: // Name Table Address for Layer A
                    NameTableAddrA = (ushort) ((data & 0x38) << 10);
                    Console.WriteLine("SET NTa A = {0:X4}",NameTableAddrA);
                    break;
                case 0x03: // Name Table Address for Window
                    NameTableAddrWindow = (ushort) ((data & 0x3E) << 10);
                    Console.WriteLine("SET NTa W = {0:X4}", NameTableAddrWindow);
                    break;
                case 0x04: // Name Table Address for Layer B
                    NameTableAddrB = (ushort) (data << 13);
                    Console.WriteLine("SET NTa B = {0:X4}", NameTableAddrB);
                    break;
                case 0x05: // Sprite Attribute Table Address
                    SpriteAttributeTableAddr = (ushort) (data << 9);
                    Console.WriteLine("SET SAT attr = {0:X4}", SpriteAttributeTableAddr);
                    break;
                case 0x0C: // Mode Set #4
                    // TODO interlaced modes
                    if ((data & 0x81) == 0)
                    {
                        // Display is 32 cells wide
                        if (FrameWidth != 256)
                        {
                            FrameBuffer = new int[256*224];
                            FrameWidth = 256;
                            Console.WriteLine("SWITCH TO 32 CELL WIDE MODE");
                        }
                    } else {
                        // Display is 40 cells wide
                        if (FrameWidth != 320)
                        {
                            FrameBuffer = new int[320*224];
                            FrameWidth = 320;
                            Console.WriteLine("SWITCH TO 40 CELL WIDE MODE");
                        }
                    }
                    break;
                case 0x0D: // H Scroll Table Address
                    HScrollTableAddr = (ushort) (data << 10);
                    Console.WriteLine("SET HScrollTab attr = {0:X4}", HScrollTableAddr);
                    break;
                case 0x0F:
                    Console.WriteLine("Set Data Increment to "+data);
                    break;
                case 0x10:
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
                    Console.WriteLine("Name Table Dimensions set to {0}x{1}", NameTableWidth, NameTableHeight);
                    break;
            }
            Registers[register] = data;
        }

        private void ProcessPalette(int slot)
        {
            byte r = PalXlatTable[(CRAM[slot] & 0x000F) >> 0];
            byte g = PalXlatTable[(CRAM[slot] & 0x00F0) >> 4];
            byte b = PalXlatTable[(CRAM[slot] & 0x0F00) >> 8];
            Palette[slot] = Colors.ARGB(r, g, b);
        }

        private void UpdatePatternBuffer(int addr)
        {
            PatternBuffer[(addr*2) + 1] = (byte) (VRAM[addr^1] & 0x0F);
            PatternBuffer[(addr*2) + 0] = (byte) (VRAM[addr^1] >> 4);
        }

        public int[] GetVideoBuffer()
        {
            return FrameBuffer;
        }

        public int BufferWidth
        {
            get { return FrameWidth; }
        }

        public int BufferHeight
        {
            get { return 224; }
        }

        public int BackgroundColor
        {
            get { return Palette[Registers[7] & 0x3F]; }
        }
    }
}