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
        internal bool DmaRequested;
        internal bool SatDmaRequested;
        internal bool SatDmaPerformed;

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

        public bool BackgroundEnabled               { get { return (Registers[CR] & 0x80) != 0; } }
        public bool SpritesEnabled                  { get { return (Registers[CR] & 0x40) != 0; } }
        public bool VBlankInterruptEnabled          { get { return (Registers[CR] & 0x08) != 0; } }
        public bool RasterCompareInterruptEnabled   { get { return (Registers[CR] & 0x04) != 0; } }
        public bool SpriteOverflowInterruptEnabled  { get { return (Registers[CR] & 0x02) != 0; } }
        public bool SpriteCollisionInterruptEnabled { get { return (Registers[CR] & 0x01) != 0; } }
        public bool Sprite4ColorModeEnabled         { get { return (Registers[MWR] & 0x0C) == 4; } }

        public int BatWidth  { get { switch((Registers[MWR] >> 4) & 3) { case 0: return 32; case 1: return 64; default: return 128; } } }
        public int BatHeight { get { return ((Registers[MWR] & 0x40) == 0) ? 32 : 64; } }

        public int RequestedFrameWidth  { get { return ((Registers[HDR] & 0x3F) + 1) * 8; } }
        public int RequestedFrameHeight { get { return ((Registers[VDW] & 0x1FF) + 1); } }

        public int DisplayStartLine { get { return (Registers[VPR] >> 8) + (Registers[VPR] & 0x1F); } }

        const int MAWR = 0;  // Memory Address Write Register
        const int MARR = 1;  // Memory Address Read Register
        const int VRR  = 2;  // VRAM Read Register
        const int VWR  = 2;  // VRAM Write Register
        const int CR   = 5;  // Control Register
        const int RCR  = 6;  // Raster Compare Register
        const int BXR  = 7;  // Background X-scroll Register
        const int BYR  = 8;  // Background Y-scroll Register
        const int MWR  = 9;  // Memory-access Width Register
        const int HSR  = 10; // Horizontal Sync Register
        const int HDR  = 11; // Horizontal Display Register
        const int VPR  = 12; // Vertical synchronous register
        const int VDW  = 13; // Vertical display register
        const int VCR  = 14; // Vertical display END position register;
        const int DCR  = 15; // DMA Control Register
        const int SOUR = 16; // Source address for DMA
        const int DESR = 17; // Destination address for DMA
        const int LENR = 18; // Length of DMA transfer. Writing this will initiate DMA.
        const int SATB = 19; // Sprite Attribute Table base location in VRAM

        const int RegisterSelect = 0;
        const int LSB = 2;
        const int MSB = 3;

        public const byte StatusVerticalBlanking    = 0x20;
        public const byte StatusVramVramDmaComplete = 0x10;
        public const byte StatusVramSatDmaComplete  = 0x08;
        public const byte StatusRasterCompare       = 0x04;
        public const byte StatusSpriteOverflow      = 0x02;
        public const byte StatusSprite0Collision    = 0x01;

        const int VramSize = 0x8000;

        PCEngine pce;
        HuC6280 cpu;
        VCE vce;

        public int MultiResHack = 0;

        public VDC(PCEngine pce, HuC6280 cpu, VCE vce)
        {
            this.pce = pce;
            this.cpu = cpu;
            this.vce = vce;

            Registers[HSR] = 0x00FF;
            Registers[HDR] = 0x00FF;
            Registers[VPR] = 0xFFFF;
            Registers[VCR] = 0xFFFF;
            ReadBuffer = 0xFFFF;
        }

        public void WriteVDC(int port, byte value)
        {
            cpu.PendingCycles--;
            port &= 3;
            if (port == RegisterSelect)
            {
                RegisterLatch = (byte)(value & 0x1F);
            }
            else if (port == LSB)
            {
                Registers[RegisterLatch] &= 0xFF00;
                Registers[RegisterLatch] |= value;

                if (RegisterLatch == BYR)
                    BackgroundY = Registers[BYR] & 0x1FF;
            }
            else if (port == MSB)
            {
                Registers[RegisterLatch] &= 0x00FF;
                Registers[RegisterLatch] |= (ushort) (value << 8);
                CompleteMSBWrite(RegisterLatch);
            }
        }

        void CompleteMSBWrite(int register)
        {
            switch (register)
            {
                case MARR: // Memory Address Read Register
                    ReadBuffer = VRAM[Registers[MARR] & 0x7FFF];
                    break;
                case VWR: // VRAM Write Register
                    if (Registers[MAWR] < VramSize) // Several games attempt to write past the end of VRAM
                    {
                        VRAM[Registers[MAWR]] = Registers[VWR];
                        UpdatePatternData((ushort) (Registers[MAWR] & 0x7FFF));
                        UpdateSpriteData((ushort) (Registers[MAWR] & 0x7FFF));
                    }
                    Registers[MAWR] += IncrementWidth;
                    break;
                case BXR:
                    Registers[BXR] &= 0x3FF;
                    break;
                case BYR:
                    Registers[BYR] &= 0x1FF;
                    BackgroundY = Registers[BYR];
                    break;
                case HDR: // Horizontal Display Register - update framebuffer size
                    FrameWidth = RequestedFrameWidth;
                    FramePitch = MultiResHack == 0 ? FrameWidth : MultiResHack;
                    if (FrameBuffer.Length != FramePitch * FrameHeight)
                        FrameBuffer = new int[FramePitch * FrameHeight];
                    break;
                case VDW: // Vertical Display Word? - update framebuffer size
                    FrameHeight = RequestedFrameHeight;
                    FrameWidth = RequestedFrameWidth;
                    if (FrameHeight > 242)
                        FrameHeight = 242;
                    if (MultiResHack != 0)
                        FramePitch = MultiResHack;
                    if (FrameBuffer.Length != FramePitch * FrameHeight)
                        FrameBuffer = new int[FramePitch * FrameHeight];
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
                    StatusByte = 0; // maybe bit 6 should be preserved. but we dont currently emulate it.
                    cpu.IRQ1Assert = false;
                    return retval;
                case 1: // unused
                    return 0;
                case 2: // LSB
                    return (byte) ReadBuffer;
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

        internal void RunDmaForScanline()
        {
            // TODO: dont do this all in one scanline. I guess it can do about 227 words per scanline.
            // TODO: to be honest, dont do it in a block per scanline. put it in the CPU think function.
            Console.WriteLine("******************************* Doing some dma ******************************");
            int advanceSource = (Registers[DCR] & 4) == 0 ? +1 : -1;
            int advanceDest   = (Registers[DCR] & 8) == 0 ? +1 : -1;
            int wordsDone = 0;

            for (;Registers[LENR]<0xFFFF;Registers[LENR]--,wordsDone++)
            {
                VRAM[Registers[DESR] & 0x7FFF] = VRAM[Registers[SOUR] & 0x7FFF];
                UpdatePatternData(Registers[DESR]);
                UpdateSpriteData(Registers[DESR]);
                Registers[DESR] = (ushort)(Registers[DESR] + advanceDest);
                Registers[SOUR] = (ushort)(Registers[SOUR] + advanceSource);

                /*if (wordsDone == 227) {
                    Console.WriteLine("ended dma for this scanline");
                    return;
                }*/
            }

            DmaRequested = false;
            //Console.WriteLine("DMA finished");

            if ((Registers[DCR] & 2) > 0)
            {
                //Log.Note("Vdc","FIRE VRAM-VRAM DMA COMPLETE IRQ");
                StatusByte |= StatusVramVramDmaComplete;
                cpu.IRQ1Assert = true;
            }
        }

        public void UpdateSpriteAttributeTable()
        {
            if ((SatDmaRequested || (Registers[DCR] & 0x10) != 0) && Registers[SATB] <= 0x7F00)
            {
                SatDmaRequested = false;
                SatDmaPerformed = true;
                for (int i = 0; i < 256; i++)
                {
                    SpriteAttributeTable[i] = VRAM[Registers[SATB] + i];
                }
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

            writer.WriteLine("DmaRequested {0}", DmaRequested);
            writer.WriteLine("SatDmaRequested {0}", SatDmaRequested);
            writer.WriteLine("SatDmaPerformed {0}", SatDmaPerformed);

            writer.WriteLine("ScanLine {0}", ScanLine);
            writer.WriteLine("BackgroundY {0}", BackgroundY);
            writer.WriteLine("RCRCounter {0}", RCRCounter);
            writer.WriteLine("ActiveLine {0}", ActiveLine);

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

                else if (args[0] == "DmaRequested")
                    DmaRequested = bool.Parse(args[1]);
                else if (args[0] == "SatDmaRequested")
                    SatDmaRequested = bool.Parse(args[1]);
                else if (args[0] == "SatDmaPerformed")
                    SatDmaPerformed = bool.Parse(args[1]);

                else if (args[0] == "ScanLine")
                    ScanLine = int.Parse(args[1]);
                else if (args[0] == "BackgroundY")
                    BackgroundY = int.Parse(args[1]);
                else if (args[0] == "RCRCounter")
                    RCRCounter = int.Parse(args[1]);
                else if (args[0] == "ActiveLine")
                    ActiveLine = int.Parse(args[1]);

                else
                    Console.WriteLine("Skipping unrecognized identifier " + args[0]);
            }

            for (ushort i = 0; i < VRAM.Length; i++)
            {
                UpdatePatternData(i);
                UpdateSpriteData(i);
            }

            CompleteMSBWrite(HDR);
            CompleteMSBWrite(VDW);
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

            writer.Write(DmaRequested);
            writer.Write(SatDmaRequested);
            writer.Write(SatDmaPerformed);

            writer.Write(ScanLine);
            writer.Write(BackgroundY);
            writer.Write(RCRCounter);
            writer.Write(ActiveLine);
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

            DmaRequested = reader.ReadBoolean();
            SatDmaRequested = reader.ReadBoolean();
            SatDmaPerformed = reader.ReadBoolean();

            ScanLine = reader.ReadInt32();
            BackgroundY = reader.ReadInt32();
            RCRCounter = reader.ReadInt32();
            ActiveLine = reader.ReadInt32();

            CompleteMSBWrite(HDR);
            CompleteMSBWrite(VDW);
        }
    }
}
