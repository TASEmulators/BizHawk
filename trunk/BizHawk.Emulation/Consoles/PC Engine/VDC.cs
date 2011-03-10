using System;
using System.Globalization;
using System.IO;
using BizHawk.Emulation.CPUs.H6280;

namespace BizHawk.Emulation.Consoles.TurboGrafx
{
    // HuC6270 Video Display Controller

    public sealed partial class VDC : IVideoProvider
    {
        public ushort[] VRAM = new ushort[0x8000];
        public ushort[] SpriteAttributeTable = new ushort[256];
        public byte[] PatternBuffer = new byte[0x20000];
        public byte[] SpriteBuffer = new byte[0x20000];
        public byte RegisterLatch;
        public ushort[] Registers = new ushort[0x20];
        public ushort ReadBuffer;
        public byte StatusByte;
        private bool DmaRequested;
        private bool SatDmaRequested;

        public ushort IncrementWidth
        {
            get
            {
                switch ((Registers[5] >> 11) & 3)
                {
                    case 0: return 1;
                    case 1: return 32;
                    case 2: return 64;
                    case 3: return 128;
                }
                return 1;
            }
        }

        public bool BackgroundEnabled  { get { return (Registers[CR] & 0x80) != 0; } }
        public bool SpritesEnabled     { get { return (Registers[CR] & 0x40) != 0; } }
        public bool IntVerticalBlank   { get { return (Registers[CR] & 0x08) != 0; } }
        public bool IntRasterCompare   { get { return (Registers[CR] & 0x04) != 0; } }
        public bool IntSpriteOverflow  { get { return (Registers[CR] & 0x02) != 0; } }
        public bool IntSpriteCollision { get { return (Registers[CR] & 0x01) != 0; } }

        public int BatWidth  { get { switch((Registers[MWR] >> 4) & 3) { case 0: return 32; case 1: return 64; default: return 128; } } }
        public int BatHeight { get { return ((Registers[MWR] & 0x40) == 0) ? 32 : 64; } }

        public int RequestedFrameWidth  { get { return ((Registers[HDR] & 0x3F) + 1) * 8; } }
        public int RequestedFrameHeight { get { return ((Registers[VDW] & 0x1FF) + 1); } }

        public int DisplayStartLine { get { return (Registers[VPR] >> 8) + (Registers[VPR] & 0x1F); } }

        private const int MAWR = 0;  // Memory Address Write Register
        private const int MARR = 1;  // Memory Address Read Register
        private const int VRR  = 2;  // VRAM Read Register
        private const int VWR  = 2;  // VRAM Write Register
        private const int CR   = 5;  // Control Register
        private const int RCR  = 6;  // Raster Compare Register
        private const int BXR  = 7;  // Background X-scroll Register
        private const int BYR  = 8;  // Background Y-scroll Register
        private const int MWR  = 9;  // Memory-access Width Register
        private const int HSR  = 10; // Horizontal Sync Register
        private const int HDR  = 11; // Horizontal Display Register
        private const int VPR  = 12; // Vertical synchronous register
        private const int VDW  = 13; // Vertical display register
        private const int VCR  = 14; // Vertical display END position register;
        private const int DCR  = 15; // DMA Control Register
        private const int SOUR = 16; // Source address for DMA
        private const int DESR = 17; // Destination address for DMA
        private const int LENR = 18; // Length of DMA transfer. Writing this will initiate DMA.
        private const int SATB = 19; // Sprite Attribute Table base location in VRAM

        private const int RegisterSelect = 0;
        private const int LSB = 2;
        private const int MSB = 3;

        public const byte StatusVerticalBlanking    = 0x20;
        public const byte StatusVramVramDmaComplete = 0x10;
        public const byte StatusVramSatDmaComplete  = 0x08;
        public const byte StatusRasterCompare       = 0x04;
        public const byte StatusSpriteOverflow      = 0x02;
        public const byte StatusSprite0Collision    = 0x01;

        private HuC6280 cpu;
        private VCE vce;

        public VDC(HuC6280 cpu, VCE vce)
        {
            this.cpu = cpu;
            this.vce = vce;
        }

        public void WriteVDC(int port, byte value)
        {
            cpu.PendingCycles--;
            port &= 3;
            if (port == RegisterSelect)
            {
                RegisterLatch = (byte)(value & 0x1F);
                Log.Note("CPU","LATCH VDC REGISTER: {0:X}",RegisterLatch);
            }
            else if (port == LSB)
            {
                Registers[RegisterLatch] &= 0xFF00;
                Registers[RegisterLatch] |= value;
            }
            else if (port == MSB)
            {
                Registers[RegisterLatch] &= 0x00FF;
                Registers[RegisterLatch] |= (ushort) (value << 8);
                CompleteMSBWrite();
            }
        }

        private void CompleteMSBWrite()
        {
            switch (RegisterLatch)
            {
                case MARR: // Memory Address Read Register
                    ReadBuffer = VRAM[Registers[MARR] & 0x7FFF];
                    break;
                case VWR: // VRAM Write Register
                    if (Registers[MAWR] < 0x8000)
                    {
                        VRAM[Registers[MAWR] & 0x7FFF] = Registers[VWR];
                        UpdatePatternData((ushort) (Registers[MAWR] & 0x7FFF));
                        UpdateSpriteData((ushort) (Registers[MAWR] & 0x7FFF));
                    }
                    Registers[MAWR] += IncrementWidth;
                    break;
case CR:
//if (Registers[CR] == 0)
//Log.Note("CPU", "****************** WRITE TO CR: {0:X}", Registers[CR]);
break;
                case BXR:
                    Registers[BXR] &= 0x3FF;
                    break;
                case BYR:
                    Registers[BYR] &= 0x1FF;
                    BackgroundY = Registers[BYR];
                    //Console.WriteLine("Updating BYR to {0} at scanline {1}", BackgroundY, ScanLine);
                    break;
                case HDR: // Horizontal Display Register - update framebuffer size
                    FrameWidth = RequestedFrameWidth;
                    if (FrameBuffer.Length != FrameWidth * FrameHeight)
                    {
                        FrameBuffer = new int[FrameWidth*FrameHeight];
                        Console.WriteLine("RESIZED FRAME BUFFER: width="+FrameWidth);
                    }
                    break;
                case VPR:
                    int vds = Registers[VPR] >> 8;
                    int vsw = Registers[VPR] & 0x1F;
                    Console.WriteLine("SET VPR: VDS {0} VSW {1} startpos={2} {3}",vds, vsw, vds+vsw, DisplayStartLine);
                    break;
                case VDW: // Vertical Display Word? - update framebuffer size
                    Console.WriteLine("REQUEST FRAME HEIGHT=" + RequestedFrameHeight);
                    FrameHeight = RequestedFrameHeight;
                    if (FrameBuffer.Length != FrameWidth * FrameHeight)
                    {
                        FrameBuffer = new int[FrameWidth * FrameHeight];
                        Console.WriteLine("RESIZED FRAME BUFFER: height="+FrameHeight);
                    }
                    break;
                case VCR: 
                    Console.WriteLine("VCR / END POSITION: "+(Registers[VCR] & 0xFF));
                    break;
                case LENR: // Initiate DMA transfer
                    DmaRequested = true;
                    break;
                case SATB:
                    SatDmaRequested = true;
                    break;
            }
        }

        public byte ReadVDC(int port)
        {
            cpu.PendingCycles--;
            byte retval = 0;

            port &= 3;
            switch (port)
            {
                case 0: // return status byte;
                    retval = StatusByte;
                    StatusByte = 0; // TODO maybe bit 6 should be preserved. but we dont currently emulate it.
                    cpu.IRQ1Assert = false;
                    return retval;
                case 1: // unused
                    return 0;
                case 2: // LSB
                    return (byte) (ReadBuffer & 0xFF);
                case 3: // MSB
                    retval = (byte)(ReadBuffer >> 8);
                    if (RegisterLatch == VRR)
                    {
                        Registers[MARR] += IncrementWidth;
                        ReadBuffer = VRAM[Registers[MARR]&0x7FFF];
                    }
                    return retval;
            }
            return 0;
        }

        private void RunDmaForScanline()
        {
            Console.WriteLine("DOING DMA ********************************************* ");
            DmaRequested = false;
            int advanceSource = (Registers[DCR] & 4) == 0 ? +1 : -1;
            int advanceDest   = (Registers[DCR] & 8) == 0 ? +1 : -1;

            for (;Registers[LENR]<0xFFFF;Registers[LENR]--)
            {
                VRAM[Registers[DESR] & 0x7FFF] = VRAM[Registers[SOUR] & 0x7FFF];
                UpdatePatternData(Registers[DESR]);
                UpdateSpriteData(Registers[DESR]);
                Registers[DESR] = (ushort)(Registers[DESR] + advanceDest);
                Registers[SOUR] = (ushort)(Registers[SOUR] + advanceSource);
            }

            if ((Registers[DCR] & 2) > 0)
            {
                Console.WriteLine("FIRE VRAM-VRAM DMA COMPLETE IRQ");
                StatusByte |= StatusVramVramDmaComplete;
                cpu.IRQ1Assert = true;
            }
        }

        public void UpdatePatternData(ushort addr)
        {
            int tileNo = (addr >> 4);
            int tileLineOffset = (addr & 0x7);

            int bitplane01 = VRAM[(tileNo * 16) + tileLineOffset];
            int bitplane23 = VRAM[(tileNo * 16) + tileLineOffset + 8];

            int patternBufferBase = (tileNo * 64) + (tileLineOffset * 8);

            for (int x = 0; x < 8; x++)
            {
                byte pixel = (byte) ((bitplane01 >> x) & 1);
                pixel |= (byte) (((bitplane01 >> (x + 8)) & 1) << 1);
                pixel |= (byte) (((bitplane23 >> x) & 1) << 2);
                pixel |= (byte) (((bitplane23 >> (x + 8)) & 1) << 3);
                PatternBuffer[patternBufferBase + (7 - x)] = pixel;
            }
        }

        public void UpdateSpriteData(ushort addr)
        {
            int tileNo = addr >> 6;
            int tileOfs = addr & 0x3F;
            int bitplane = tileOfs/16;
            int line = addr & 0x0F;

            int ofs = (tileNo*256) + (line*16) + 15;
            ushort value = VRAM[addr];
            byte bitAnd = (byte) (~(1 << bitplane));
            byte bitOr = (byte) (1 << bitplane);
            
            for (int i=0; i<16; i++)
            {

                if ((value & 1) == 1)
                    SpriteBuffer[ofs] |= bitOr;
                else
                    SpriteBuffer[ofs] &= bitAnd;
                ofs--;
                value >>= 1;
            }
        }

        public void SaveStateText(TextWriter writer, int vdcNo)
        {
            writer.WriteLine("[VDC"+vdcNo+"]");
            writer.Write("VRAM ");
            VRAM.SaveAsHex(writer);
            writer.Write("SAT ");
            SpriteAttributeTable.SaveAsHex(writer);
            writer.Write("Registers ");
            Registers.SaveAsHex(writer);

            writer.WriteLine("RegisterLatch {0:X2}", RegisterLatch);
            writer.WriteLine("ReadBuffer {0:X4}", ReadBuffer);
            writer.WriteLine("StatusByte {0:X2}", StatusByte);
            writer.WriteLine("[/VDC"+vdcNo+"]\n");
        }

        public void LoadStateText(TextReader reader, int vdcNo)
        {
            while (true)
            {
                string[] args = reader.ReadLine().Split(' ');
                if (args[0].Trim() == "") continue;
                if (args[0] == "[/VDC"+vdcNo+"]") break;
                if (args[0] == "VRAM")
                    VRAM.ReadFromHex(args[1]);
                else if (args[0] == "SAT")
                    SpriteAttributeTable.ReadFromHex(args[1]);
                else if (args[0] == "Registers")
                    Registers.ReadFromHex(args[1]);
                else if (args[0] == "RegisterLatch")
                    RegisterLatch = byte.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "ReadBuffer")
                    ReadBuffer = ushort.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "StatusByte")
                    StatusByte = byte.Parse(args[1], NumberStyles.HexNumber);
                else
                    Console.WriteLine("Skipping unrecognized identifier " + args[0]);
            }

            for (ushort i = 0; i < VRAM.Length; i++)
            {
                UpdatePatternData(i);
                UpdateSpriteData(i);
            }
        }

        public void SaveStateBinary(BinaryWriter writer)
        {
            for (int i=0; i < VRAM.Length; i++)
                writer.Write(VRAM[i]);
            for (int i=0; i < SpriteAttributeTable.Length; i++)
                writer.Write(SpriteAttributeTable[i]);
            for (int i = 0; i < Registers.Length; i++)
                writer.Write(Registers[i]);
            writer.Write(RegisterLatch);
            writer.Write(ReadBuffer);
            writer.Write(StatusByte);
        }

        public void LoadStateBinary(BinaryReader reader)
        {
            for (ushort i=0; i < VRAM.Length; i++)
            {
                VRAM[i] = reader.ReadUInt16();
                UpdatePatternData(i);
                UpdateSpriteData(i);
            }
            for (int i=0; i < SpriteAttributeTable.Length; i++)
                SpriteAttributeTable[i] = reader.ReadUInt16();
            for (int i=0; i < Registers.Length; i++)
                Registers[i] = reader.ReadUInt16();
            RegisterLatch = reader.ReadByte();
            ReadBuffer = reader.ReadUInt16();
            StatusByte = reader.ReadByte();
        }
    }
}
