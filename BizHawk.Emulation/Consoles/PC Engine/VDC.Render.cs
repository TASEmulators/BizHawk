using System;

namespace BizHawk.Emulation.Consoles.TurboGrafx
{
    // This rendering code is only used for TurboGrafx/TurboCD Mode.
    // In SuperGrafx mode, separate rendering functions in the VPC class are used.

    public partial class VDC
    {
        /* There are many line-counters here. Here is a breakdown of what they each are:
        
         + ScanLine is the current NTSC scanline. It has a range from 0 to 262.
         + ActiveLine is the current offset into the framebuffer. 0 is the first
           line of active display, and the last value will be BufferHeight-1.
         + BackgroundY is the current offset into the scroll plane. It is set with BYR
           register at certain sync points and incremented every scanline. 
           Its values range from 0 - $1FF.
        */
        public int ScanLine;
        public int BackgroundY;

        private byte[] PriorityBuffer = new byte[512];
        private byte[] InterSpritePriorityBuffer = new byte[512];
        private int latchedDisplayStartLine;
        private int ActiveLine;

        public void ExecFrame(bool render)
        {
            for (ScanLine = 0; ScanLine < 263;)
            {
                latchedDisplayStartLine = DisplayStartLine;
                ActiveLine = ScanLine - latchedDisplayStartLine;

                int vds = Registers[VPR] >> 8;
                int vsw = Registers[VPR] & 0x1F;

                int VBlankScanline = vds + vsw + Registers[VDW] + 1;
                if (VBlankScanline > 261)
                    VBlankScanline = 261;

                const int hblankCycles = 79;

                if (ActiveLine == 0)
                    BackgroundY = Registers[BYR];

                if (ActiveLine == (Registers[RCR] & 0x3FF) - 0x40)
                {
                    if (RasterCompareInterruptEnabled)
                    {
                        Log.Note("VDC", "Firing RCR interrupt at {0}", ScanLine);
                        StatusByte |= StatusRasterCompare;
                        cpu.IRQ1Assert = true;
                    }
                }

                cpu.Execute(hblankCycles);

                bool InActiveDisplay = false;
                if (ScanLine >= latchedDisplayStartLine && ScanLine < latchedDisplayStartLine + FrameHeight)
                    InActiveDisplay = true;

                if (InActiveDisplay)
                {
                    if (ActiveLine == 0)
                        BackgroundY = Registers[BYR];
                    else
                    {
                        BackgroundY++;
                        BackgroundY &= 0x01FF;
                    }
                    if (render) RenderScanLine();
                }

                if (ScanLine == VBlankScanline && VBlankInterruptEnabled)
                    StatusByte |= StatusVerticalBlanking;

                cpu.Execute(2);

                if ((StatusByte & StatusVerticalBlanking) > 0)
                    cpu.IRQ1Assert = true;

                cpu.Execute(455 - hblankCycles - 2);

                if (ScanLine == VBlankScanline)
                    UpdateSpriteAttributeTable();

                if (InActiveDisplay == false && DmaRequested)
                    RunDmaForScanline();

                ScanLine++;
                ActiveLine++;
            }
        }

        public void RenderScanLine()
        {
            RenderBackgroundScanline();
            RenderSpritesScanline();
        }

        public void UpdateSpriteAttributeTable()
        {
            if ((SatDmaRequested || (Registers[DCR] & 0x10) != 0) && Registers[SATB] <= 0x7F00)
            {
                SatDmaRequested = false;
                for (int i = 0; i < 256; i++)
                {
                    SpriteAttributeTable[i] = VRAM[Registers[SATB] + i];
                }

                if ((Registers[DCR] & 1) > 0)
                {
                    Log.Note("VDC","FIRING SATB DMA COMPLETION IRQ");
                    StatusByte |= StatusVramSatDmaComplete;
                    cpu.IRQ1Assert = true;
                }
            }
        }

        private void RenderBackgroundScanline()
        {
            Array.Clear(PriorityBuffer, 0, FrameWidth);

            if (BackgroundEnabled == false)
            {
                for (int i = 0; i < FrameWidth; i++)
                    FrameBuffer[(ActiveLine * FrameWidth) + i] = vce.Palette[0];
                return;
            }

            int batHeight = BatHeight * 8;
            int batWidth = BatWidth * 8;

            int vertLine = BackgroundY;
            vertLine %= batHeight;
            int yTile = (vertLine / 8);
            int yOfs = vertLine % 8;

            // TODO: x-scrolling is done super quick and shitty here and slow.
            // Ergo, make it better later.

            int xScroll = Registers[BXR] & 0x3FF;
            for (int x = 0; x < FrameWidth; x++)
            {
                int xTile = ((x + xScroll) / 8) % BatWidth;
                int xOfs = (x + xScroll) & 7;
                int tileNo = VRAM[(ushort)(((yTile * BatWidth) + xTile))] & 2047;
                int paletteNo = VRAM[(ushort)(((yTile * BatWidth) + xTile))] >> 12;
                int paletteBase = paletteNo * 16;

                byte c = PatternBuffer[(tileNo * 64) + (yOfs * 8) + xOfs];
                if (c == 0)
                    FrameBuffer[(ActiveLine * FrameWidth) + x] = vce.Palette[0];
                else
                {
                    FrameBuffer[(ActiveLine * FrameWidth) + x] = vce.Palette[paletteBase + c];
                    PriorityBuffer[x] = 1;
                }
            }
        }

        private byte[] heightTable = { 16, 32, 64, 64 };

        public void RenderSpritesScanline()
        {
            if (SpritesEnabled == false)
                return;

            Array.Clear(InterSpritePriorityBuffer, 0, FrameWidth);

            for (int i = 0; i < 64; i++)
            {
                int y = (SpriteAttributeTable[(i * 4) + 0] & 1023) - 64;
                int x = (SpriteAttributeTable[(i * 4) + 1] & 1023) - 32;
                ushort flags = SpriteAttributeTable[(i * 4) + 3];
                int height = heightTable[(flags >> 12) & 3];

                if (y + height <= ActiveLine || y > ActiveLine)
                    continue;

                int patternNo = (((SpriteAttributeTable[(i * 4) + 2]) >> 1) & 0x1FF);
                int paletteBase = 256 + ((flags & 15) * 16);
                int width = (flags & 0x100) == 0 ? 16 : 32;
                bool priority = (flags & 0x80) != 0;
                bool hflip = (flags & 0x0800) != 0;
                bool vflip = (flags & 0x8000) != 0;

                if (width == 32)
                    patternNo &= 0x1FE;

                int yofs = 0;
                if (vflip == false)
                {
                    yofs = (ActiveLine - y) & 15;
                    if (height == 32)
                    {
                        patternNo &= 0x1FD;
                        if (ActiveLine - y >= 16)
                        {
                            y += 16;
                            patternNo += 2;
                        }
                    }
                    else if (height == 64)
                    {
                        patternNo &= 0x1F9;
                        if (ActiveLine - y >= 48)
                        {
                            y += 48;
                            patternNo += 6;
                        }
                        else if (ActiveLine - y >= 32)
                        {
                            y += 32;
                            patternNo += 4;
                        }
                        else if (ActiveLine - y >= 16)
                        {
                            y += 16;
                            patternNo += 2;
                        }
                    }
                }
                else // vflip == true
                {
                    yofs = 15 - ((ActiveLine - y) & 15);
                    if (height == 32)
                    {
                        patternNo &= 0x1FD;
                        if (ActiveLine - y < 16)
                        {
                            y += 16;
                            patternNo += 2;
                        }
                    }
                    else if (height == 64)
                    {
                        patternNo &= 0x1F9;
                        if (ActiveLine - y < 16)
                        {
                            y += 48;
                            patternNo += 6;
                        }
                        else if (ActiveLine - y < 32)
                        {
                            y += 32;
                            patternNo += 4;
                        }
                        else if (ActiveLine - y < 48)
                        {
                            y += 16;
                            patternNo += 2;
                        }
                    }
                }
                if (hflip == false)
                {
                    if (x + width > 0 && y + height > 0)
                    {
                        for (int xs = x >= 0 ? x : 0; xs < x + 16 && xs >= 0 && xs < FrameWidth; xs++)
                        {
                            byte pixel = SpriteBuffer[(patternNo * 256) + (yofs * 16) + (xs - x)];
                            if (pixel != 0 && InterSpritePriorityBuffer[xs] == 0)
                            {
                                InterSpritePriorityBuffer[xs] = 1;
                                if (priority || PriorityBuffer[xs] == 0)
                                    FrameBuffer[(ActiveLine * FrameWidth) + xs] = vce.Palette[paletteBase + pixel];
                            }
                        }
                    }
                    if (width == 32)
                    {
                        patternNo++;
                        x += 16;
                        for (int xs = x >= 0 ? x : 0; xs < x + 16 && xs >= 0 && xs < FrameWidth; xs++)
                        {
                            byte pixel = SpriteBuffer[(patternNo * 256) + (yofs * 16) + (xs - x)];
                            if (pixel != 0 && InterSpritePriorityBuffer[xs] == 0)
                            {
                                InterSpritePriorityBuffer[xs] = 1;
                                if (priority || PriorityBuffer[xs] == 0)
                                    FrameBuffer[(ActiveLine * FrameWidth) + xs] = vce.Palette[paletteBase + pixel];
                            }

                        }
                    }
                }
                else
                { // hflip = true
                    if (x + width > 0 && y + height > 0)
                    {
                        if (width == 32)
                            patternNo++;
                        for (int xs = x >= 0 ? x : 0; xs < x + 16 && xs >= 0 && xs < FrameWidth; xs++)
                        {
                            byte pixel = SpriteBuffer[(patternNo * 256) + (yofs * 16) + 15 - (xs - x)];
                            if (pixel != 0 && InterSpritePriorityBuffer[xs] == 0)
                            {
                                InterSpritePriorityBuffer[xs] = 1;
                                if (priority || PriorityBuffer[xs] == 0)
                                    FrameBuffer[(ActiveLine * FrameWidth) + xs] = vce.Palette[paletteBase + pixel];
                            }
                        }
                        if (width == 32)
                        {
                            patternNo--;
                            x += 16;
                            for (int xs = x >= 0 ? x : 0; xs < x + 16 && xs >= 0 && xs < FrameWidth; xs++)
                            {
                                byte pixel = SpriteBuffer[(patternNo * 256) + (yofs * 16) + 15 - (xs - x)];
                                if (pixel != 0 && InterSpritePriorityBuffer[xs] == 0)
                                {
                                    InterSpritePriorityBuffer[xs] = 1;
                                    if (priority || PriorityBuffer[xs] == 0)
                                        FrameBuffer[(ActiveLine * FrameWidth) + xs] = vce.Palette[paletteBase + pixel];
                                }
                            }
                        }
                    }
                }
            }
        }

        private int FrameWidth = 256;
        private int FrameHeight = 240;
        private int[] FrameBuffer = new int[256 * 240];

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
            get { return FrameHeight; }
        }

        public int BackgroundColor
        {
            get { return vce.Palette[256]; }
        }
    }
}
