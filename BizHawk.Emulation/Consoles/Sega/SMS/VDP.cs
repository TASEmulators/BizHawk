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
    public sealed partial class VDP : IVideoProvider
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

        private int FrameHeight = 192;

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
        public int SpriteAttributeTableBase { get { return ((Registers[5] >> 1) << 8) & 0x3FFF; } }
        public int SpriteTileBase           { get { return (Registers[6] & 4) > 0 ? 256: 0; } }
        
        public int NameTableBase 
        { 
            get
            {
                if (FrameHeight == 192) 
                    return 1024 * (Registers[2] & 0x0E);
                return (1024 * (Registers[2] & 0x0C)) + 0x0700;
            } 
        }

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
            return FrameHeight == 240 ? VLineCounterTableNTSC240[ScanLine] : VLineCounterTableNTSC192[ScanLine];
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
                    int reg = value & 0x0F;
                    Registers[reg] = vdpLatch;
                    if (reg == 1 || reg == 2)
                        CheckVideoMode();
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

        private void CheckVideoMode()
        {
            if ((Registers[0] & 6) == 6) // if Mode4 and Mode2 set, then check extension modes
            {
                switch (Registers[1] & 0x18)
                {
                    case 0x00:
                    case 0x18: // 192-line mode
                        if (FrameHeight != 192)
                        {
                            FrameHeight = 192;
                            FrameBuffer = new int[256*192];
                        }
                        break;
                    case 0x10: // 224-line mode
                        if (FrameHeight != 224)
                        {
                            FrameHeight = 224;
                            FrameBuffer = new int[256*224];
                        }
                        break;
                    case 0x08: // 240-line mode
                        if (FrameHeight != 240)
                        {
                            FrameHeight = 240;
                            FrameBuffer = new int[256 * 240];
                        }
                        break;
                }
            } else { // default to standard 192-line mode4
                if (FrameHeight != 192)
                {
                    FrameHeight = 192;
                    FrameBuffer = new int[256*192];
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
            if (ScanLine >= FrameHeight)
                return;

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
            if (FrameHeight == 192)
            {
                if (vertOffset >= 224)
                    vertOffset -= 224;
            } else
            {
                if (vertOffset >= 256)
                    vertOffset -= 256;
            }
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

            // 208 is a special terminator sprite (in 192-line mode). Lets find it...
            int TerminalSprite = 64;
            if (FrameHeight == 192)
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
                for (int y=0; y<FrameHeight; y++)
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
            CheckVideoMode();
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
            CheckVideoMode();
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
            get { return mode == VdpMode.SMS ? FrameHeight : 144; }
        }

        public int BackgroundColor
        {
            get { return Palette[BackdropColor]; }
        }
    }
}