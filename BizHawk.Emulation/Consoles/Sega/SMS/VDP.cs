using System;
using System.Globalization;
using System.IO;

namespace BizHawk.Emulation.Consoles.Sega
{
    public enum VdpCommand
    {
        VramRead,
        VramWrite,
        RegisterWrite,
        CramWrite
    }
    
    public enum VdpMode
    {
        SMS,
        GameGear
    }

    /// <summary>
    /// Emulates the Texas Instruments TMS9918 VDP.
    /// </summary>
    public sealed class VDP : IVideoProvider
    {
        // VDP State
        public byte[] VRAM = new byte[0x4000]; //16kb video RAM
        public byte[] CRAM; // SMS = 32 bytes, GG = 64 bytes CRAM
        public byte[] Registers = new byte[] { 0x06, 0x80, 0xFF, 0xFF, 0xFF, 0xFF, 0xFB, 0xF0, 0x00, 0x00, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00 };
        public byte StatusByte;

        private bool vdpWaitingForLatchByte = true;
        private byte vdpLatch;
        private byte vdpBuffer;
        private ushort vdpAddress;
        private VdpCommand vdpCommand;
        private ushort vdpAddressClamp;

        private VdpMode mode;
        public VdpMode VdpMode { get { return mode; } }

        public int ScanLine;

        public int[] FrameBuffer = new int[256*192];
        public int[] GameGearFrameBuffer = new int[160*144];

        // preprocessed state assist stuff.
        public int[] Palette = new int[32];

        private static readonly byte[] SMSPalXlatTable = { 0, 85, 170, 255 };
        private static readonly byte[] GGPalXlatTable = { 0, 17, 34, 51, 68, 85, 102, 119, 136, 153, 170, 187, 204, 221, 238, 255 };

        public bool ShiftSpritesLeft8Pixels { get { return (Registers[0] & 8) > 0; } }
        public bool EnableLineInterrupts    { get { return (Registers[0] & 16) > 0; } }
        public bool LeftBlanking            { get { return (Registers[0] & 32) > 0; } }
        public bool HorizScrollLock         { get { return (Registers[0] & 64) > 0; } }
        public bool VerticalScrollLock      { get { return (Registers[0] & 128) > 0; } }
        public bool DisplayOn               { get { return (Registers[1] & 64) > 0; } }
        public bool EnableFrameInterrupts   { get { return (Registers[1] & 32) > 0; } }
        public bool Enable8x16Sprites       { get { return (Registers[1] & 2) > 0; } }
        public byte BackdropColor           { get { return (byte) (16 + (Registers[7] & 15)); } }
        public int NameTableBase            { get { return 1024 * (Registers[2] & 0x0E); } }
        public int SpriteAttributeTableBase { get { return ((Registers[5] >> 1) << 8) & 0x3FFF; } }
        public int SpriteTileBase           { get { return (Registers[6] & 4) > 0 ? 256: 0; } }

        private readonly byte[] VLineCounterTable =
        {
            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
            0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
            0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2A, 0x2B, 0x2C, 0x2D, 0x2E, 0x2F,
            0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F,
            0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F,
            0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5A, 0x5B, 0x5C, 0x5D, 0x5E, 0x5F,
            0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6A, 0x6B, 0x6C, 0x6D, 0x6E, 0x6F,
            0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7A, 0x7B, 0x7C, 0x7D, 0x7E, 0x7F,
            0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89, 0x8A, 0x8B, 0x8C, 0x8D, 0x8E, 0x8F,
            0x90, 0x91, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9A, 0x9B, 0x9C, 0x9D, 0x9E, 0x9F,
            0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7, 0xA8, 0xA9, 0xAA, 0xAB, 0xAC, 0xAD, 0xAE, 0xAF,
            0xB0, 0xB1, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6, 0xB7, 0xB8, 0xB9, 0xBA, 0xBB, 0xBC, 0xBD, 0xBE, 0xBF,
            0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xCB, 0xCC, 0xCD, 0xCE, 0xCF,
            0xD0, 0xD1, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA,
                                          0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE, 0xDF,
            0xE0, 0xE1, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xEB, 0xEC, 0xED, 0xEE, 0xEF,
            0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF,
        };

        public byte[] PatternBuffer = new byte[0x8000];

        private byte[] ScanlinePriorityBuffer = new byte[256];
        private byte[] SpriteCollisionBuffer = new byte[256];

        public VDP(VdpMode mode)
        {
            this.mode = mode;
            if (mode == VdpMode.SMS) CRAM = new byte[32];
            if (mode == VdpMode.GameGear) CRAM = new byte[64];
        }

        public byte ReadVram()
        {
            vdpWaitingForLatchByte = true;
            byte value = vdpBuffer;
            vdpBuffer = VRAM[vdpAddress & vdpAddressClamp];
            vdpAddress++;
            return value;
        }

        public byte ReadVdpStatus()
        {
            vdpWaitingForLatchByte = true;
            byte returnValue = StatusByte;
            StatusByte &= 0x1F;
            return returnValue;
        }

        public byte ReadVLineCounter()
        {
            return VLineCounterTable[ScanLine];
        }

        public void WriteVdpRegister(byte value)
        {
            if (vdpWaitingForLatchByte)
            {
                vdpLatch = value;
                vdpWaitingForLatchByte = false;
                vdpAddress = (ushort)((vdpAddress & 0xFF00) | value);
                return;
            }

            vdpWaitingForLatchByte = true;
            switch (value & 0xC0)
            {
                case 0x00: // read VRAM
                    vdpCommand = VdpCommand.VramRead;
                    vdpAddressClamp = 0x3FFF;
                    vdpAddress = (ushort)(((value & 63) << 8) | vdpLatch);
                    vdpBuffer = VRAM[vdpAddress & vdpAddressClamp];
                    vdpAddress++;
                    break;
                case 0x40: // write VRAM
                    vdpCommand = VdpCommand.VramWrite;
                    vdpAddressClamp = 0x3FFF;
                    vdpAddress = (ushort)(((value & 63) << 8) | vdpLatch);
                    break;
                case 0x80: // VDP register write
                    Registers[value & 0x0F] = vdpLatch;
                    break;
                case 0xC0: // write CRAM / modify palette
                    vdpCommand = VdpCommand.CramWrite;
                    vdpAddressClamp = (byte) (mode == VdpMode.SMS ? 0x1F : 0x3F);
                    vdpAddress = (ushort)(((value & 63) << 8) | vdpLatch);
                    break;
            }
        }

        public void WriteVdpData(byte value)
        {
            vdpWaitingForLatchByte = true;
            vdpBuffer = value;
            if (vdpCommand == VdpCommand.CramWrite)
            {
                // Write Palette / CRAM
                CRAM[vdpAddress & vdpAddressClamp] = value;
                vdpAddress++;
                UpdatePrecomputedPalette();
            }
            else
            {
                // Write VRAM and update pre-computed pattern buffer. 
                UpdatePatternBuffer((ushort)(vdpAddress & vdpAddressClamp), value);
                VRAM[vdpAddress & vdpAddressClamp] = value;
                vdpAddress++;
            }
        }

        public void UpdatePrecomputedPalette()
        {
            if (mode == VdpMode.SMS)
            {
                for (int i=0; i<32; i++)
                {
                    byte value = CRAM[i];
                    byte r = SMSPalXlatTable[(value & 0x03)];
                    byte g = SMSPalXlatTable[(value & 0x0C) >> 2];
                    byte b = SMSPalXlatTable[(value & 0x30) >> 4];
                    Palette[i] = Colors.ARGB(r, g, b);
                }
            } else // GameGear
            { 
                for (int i=0; i<32; i++)
                {
                    ushort value = (ushort) ((CRAM[(i*2) + 1] << 8) | CRAM[(i*2) + 0]);
                    byte r = GGPalXlatTable[(value & 0x000F)];
                    byte g = GGPalXlatTable[(value & 0x00F0) >> 4];
                    byte b = GGPalXlatTable[(value & 0x0F00) >> 8];
                    Palette[i] = Colors.ARGB(r, g, b);
                }
            }
        }

        private static readonly byte[] pow2 = {1, 2, 4, 8, 16, 32, 64, 128};
        
        private void UpdatePatternBuffer(ushort address, byte value)
        {
            // writing one byte affects 8 pixels due to stupid planar storage.
            for (int i=0; i<8; i++)
            {
                byte colorBit = pow2[address%4];
                byte sourceBit = pow2[7 - i];
                ushort dest = (ushort) (((address & 0xFFFC)*2) + i);
                if ((value & sourceBit) > 0) // setting bit
                    PatternBuffer[dest] |= colorBit;
                else // clearing bit
                    PatternBuffer[dest] &= (byte)~colorBit;
            }
        }

        internal void RenderCurrentScanline(bool render)
        {
            // TODO: make frameskip actually skip rendering
            RenderBackgroundCurrentLine();
            RenderSpritesCurrentLine();
        }

        internal void RenderBackgroundCurrentLine()
        {
            if (DisplayOn == false)
            {
                for (int x = 0; x < 256; x++)
                    FrameBuffer[(ScanLine*256) + x] = BackdropColor;
                return;
            }

            // Clear the priority buffer for this scanline
            for (int p = 0; p < 256; p++)
                ScanlinePriorityBuffer[p] = 0;

            int mapBase = NameTableBase;

            int vertOffset = ScanLine + Registers[9];
            if (vertOffset >= 224)
                vertOffset -= 224;
            byte horzOffset = (HorizScrollLock && ScanLine < 16) ? (byte) 0 : Registers[8];

            int yTile = vertOffset/8;

            for (int xTile = 0; xTile<32; xTile++)
            {
                if (xTile == 24 && VerticalScrollLock)
                {
                    vertOffset = ScanLine;
                    yTile = vertOffset/8;
                }
                
                byte PaletteBase = 0;
                int tileInfo = VRAM[mapBase+((yTile*32) + xTile)*2] | (VRAM[mapBase+(((yTile*32) + xTile)*2) + 1]<<8);
                int tileNo = tileInfo & 0x01FF;
                if ((tileInfo & 0x800) != 0) 
                    PaletteBase = 16;
                bool Priority = (tileInfo & 0x1000) != 0;
                bool VFlip = (tileInfo & 0x400) != 0;
                bool HFlip = (tileInfo & 0x200) != 0;

                int yOfs = vertOffset & 7;
                if (VFlip) 
                    yOfs = 7 - yOfs;

                if (HFlip == false)
                {
                    FrameBuffer[(ScanLine * 256) + horzOffset++] = Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 0] + PaletteBase];
                    FrameBuffer[(ScanLine * 256) + horzOffset++] = Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 1] + PaletteBase];
                    FrameBuffer[(ScanLine * 256) + horzOffset++] = Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 2] + PaletteBase];
                    FrameBuffer[(ScanLine * 256) + horzOffset++] = Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 3] + PaletteBase];
                    FrameBuffer[(ScanLine * 256) + horzOffset++] = Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 4] + PaletteBase];
                    FrameBuffer[(ScanLine * 256) + horzOffset++] = Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 5] + PaletteBase];
                    FrameBuffer[(ScanLine * 256) + horzOffset++] = Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 6] + PaletteBase];
                    FrameBuffer[(ScanLine * 256) + horzOffset++] = Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 7] + PaletteBase];
                    
                    if (Priority)
                    {
                        horzOffset -= 8;
                        for (int k = 0; k < 8; k++)
                        {
                            if (PatternBuffer[(tileNo * 64) + (yOfs * 8) + k] != 0)
                                ScanlinePriorityBuffer[horzOffset] = 1;
                            horzOffset++;
                        }
                    }
                }
                else // Flipped Horizontally
                {
                    FrameBuffer[(ScanLine * 256) + horzOffset++] = Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 7] + PaletteBase];
                    FrameBuffer[(ScanLine * 256) + horzOffset++] = Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 6] + PaletteBase];
                    FrameBuffer[(ScanLine * 256) + horzOffset++] = Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 5] + PaletteBase];
                    FrameBuffer[(ScanLine * 256) + horzOffset++] = Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 4] + PaletteBase];
                    FrameBuffer[(ScanLine * 256) + horzOffset++] = Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 3] + PaletteBase];
                    FrameBuffer[(ScanLine * 256) + horzOffset++] = Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 2] + PaletteBase];
                    FrameBuffer[(ScanLine * 256) + horzOffset++] = Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 1] + PaletteBase];
                    FrameBuffer[(ScanLine * 256) + horzOffset++] = Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 0] + PaletteBase];

                    if (Priority)
                    {
                        horzOffset -= 8;
                        for (int k = 7; k >= 0; k--)
                        {
                            if (PatternBuffer[(tileNo * 64) + (yOfs * 8) + k] != 0)
                                ScanlinePriorityBuffer[horzOffset] = 1;
                            horzOffset++;
                        }
                    }
                }
            }
        }

        internal void RenderSpritesCurrentLine()
        {
            if (DisplayOn == false) return;
            int SpriteBase = SpriteAttributeTableBase;
            int SpriteHeight = Enable8x16Sprites ? 16 : 8;

            // Clear the sprite collision buffer for this scanline
            for (int c = 0; c < 256; c++)
                SpriteCollisionBuffer[c] = 0;

            // 208 is a special terminator sprite. Lets find it...
            int TerminalSprite = 64;
            for (int i = 0; i < 64; i++)
            {
                if (VRAM[SpriteBase + i] == 208)
                {
                    TerminalSprite = i;
                    break;
                }
            }
            
            // Loop through these sprites and render the current scanline
            int SpritesDrawnThisScanline = 0;
            for (int i = TerminalSprite - 1; i >= 0; i--)
            {
                if (SpritesDrawnThisScanline >= 8)
                    StatusByte |= 0x40; // Set Overflow bit

                int x = VRAM[SpriteBase + 0x80 + (i*2)];
                if (ShiftSpritesLeft8Pixels)
                    x -= 8;

                int y = VRAM[SpriteBase + i] + 1;
                if (y >= (Enable8x16Sprites ? 240 : 248)) y -= 256;
                 
                if (y+SpriteHeight<=ScanLine || y > ScanLine)
                    continue;

                int tileNo = VRAM[SpriteBase + 0x80 + (i*2) + 1];
                if (Enable8x16Sprites) 
                    tileNo &= 0xFE;
                tileNo += SpriteTileBase;

                int ys = ScanLine - y;
                
                for (int xs = 0; xs<8 && x+xs < 256; xs++)
                {
                    byte color = PatternBuffer[(tileNo*64) + (ys*8) + xs];
                    if (color != 0 && x+xs >= 0 && ScanlinePriorityBuffer[x + xs] == 0)
                    {
                        FrameBuffer[(ys + y)*256 + x + xs] = Palette[(color + 16)];
                        if (SpriteCollisionBuffer[x + xs] != 0)
                            StatusByte |= 0x20; // Set Collision bit
                        SpriteCollisionBuffer[x + xs] = 1;
                    }
                }
                SpritesDrawnThisScanline++;
            }
        }

        /// <summary>
        /// Performs render buffer blanking. This includes the left-column blanking as well as Game Gear blanking if requested.
        /// Should be called at the end of the frame.
        /// </summary>
        public void RenderBlankingRegions()
        {
            int blankingColor = Palette[BackdropColor];

            if (LeftBlanking)
            {
                for (int y=0; y<192; y++)
                {
                    for (int x=0; x<8; x++)
                        FrameBuffer[(y*256) + x] = blankingColor;
                }
            }

            if (mode == VdpMode.GameGear)
            {
                for (int y = 0; y < 144; y++)
                    for (int x = 0; x < 160; x++)
                        GameGearFrameBuffer[(y*160) + x] = FrameBuffer[((y + 24)*256) + x + 48];
            }
        }

        public void SaveStateText(TextWriter writer)
        {
            writer.WriteLine("[VDP]");
            writer.WriteLine("Mode " + Enum.GetName(typeof(VdpMode), VdpMode));
            writer.WriteLine("StatusByte {0:X2}", StatusByte);
            writer.WriteLine("WaitingForLatchByte {0}", vdpWaitingForLatchByte);
            writer.WriteLine("Latch {0:X2}", vdpLatch);
            writer.WriteLine("ReadBuffer {0:X2}", vdpBuffer);
            writer.WriteLine("VdpAddress {0:X4}", vdpAddress);
            writer.WriteLine("VdpAddressMask {0:X2}", vdpAddressClamp);
            writer.WriteLine("Command " + Enum.GetName(typeof(VdpCommand), vdpCommand));

            writer.Write("Registers ");
            Registers.SaveAsHex(writer);
            writer.Write("CRAM ");
            CRAM.SaveAsHex(writer);
            writer.Write("VRAM ");
            VRAM.SaveAsHex(writer);

            writer.WriteLine("[/VDP]");
            writer.WriteLine();
        }

        public void LoadStateText(TextReader reader)
        {
            while (true)
            {
                string[] args = reader.ReadLine().Split(' ');
                if (args[0].Trim() == "") continue;
                if (args[0] == "[/VDP]") break;
                if (args[0] == "StatusByte")
                    StatusByte = byte.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "WaitingForLatchByte")
                    vdpWaitingForLatchByte = bool.Parse(args[1]);
                else if (args[0] == "Latch")
                    vdpLatch = byte.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "ReadBuffer")
                    vdpBuffer = byte.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "VdpAddress")
                    vdpAddress = ushort.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "VdpAddressMask")
                    vdpAddressClamp = ushort.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "Command")
                    vdpCommand = (VdpCommand) Enum.Parse(typeof (VdpCommand), args[1]);
                else if (args[0] == "Registers")
                    Registers.ReadFromHex(args[1]);
                else if (args[0] == "CRAM")
                {
                    CRAM.ReadFromHex(args[1]);
                    UpdatePrecomputedPalette();
                }
                else if (args[0] == "VRAM")
                {
                    VRAM.ReadFromHex(args[1]);
                    for (ushort i=0; i<VRAM.Length; i++)
                        UpdatePatternBuffer(i, VRAM[i]);
                }

                else 
                    Console.WriteLine("Skipping unrecognized identifier "+args[0]);
            }
        }

        public void SaveStateBinary(BinaryWriter writer)
        {
            writer.Write(StatusByte);
            writer.Write(vdpWaitingForLatchByte);
            writer.Write(vdpLatch);
            writer.Write(vdpBuffer);
            writer.Write(vdpAddress);
            writer.Write(vdpAddressClamp);
            writer.Write((byte)vdpCommand);
            writer.Write(Registers);
            writer.Write(CRAM);
            writer.Write(VRAM);
        }

        public void LoadStateBinary(BinaryReader reader)
        {
            StatusByte = reader.ReadByte();
            vdpWaitingForLatchByte = reader.ReadBoolean();
            vdpLatch = reader.ReadByte();
            vdpBuffer = reader.ReadByte();
            vdpAddress = reader.ReadUInt16();
            vdpAddressClamp = reader.ReadUInt16();
            vdpCommand = (VdpCommand) Enum.ToObject(typeof(VdpCommand), reader.ReadByte());
            Registers = reader.ReadBytes(Registers.Length);
            CRAM = reader.ReadBytes(CRAM.Length);
            VRAM = reader.ReadBytes(VRAM.Length);
            UpdatePrecomputedPalette();
            for (ushort i = 0; i < VRAM.Length; i++)
                UpdatePatternBuffer(i, VRAM[i]);
        }

        public int[] GetVideoBuffer()
        {
            return mode == VdpMode.SMS ? FrameBuffer : GameGearFrameBuffer;
        }

        public int BufferWidth
        {
            get { return mode == VdpMode.SMS ? 256 : 160; }
        }

        public int BufferHeight
        {
            get { return mode == VdpMode.SMS ? 192 : 144; }
        }

        public int BackgroundColor
        {
            get { return Palette[BackdropColor]; }
        }
    }
}