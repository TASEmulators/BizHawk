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
         + RCRCounter is set to $40 at the first line of active display, and incremented each
           scanline thereafter.
        */
        public int ScanLine;
        public int BackgroundY;
        public int RCRCounter;
        public int ActiveLine;

        public int HBlankCycles = 79;
        public bool PerformSpriteLimit;

        byte[] PriorityBuffer = new byte[512];
        byte[] InterSpritePriorityBuffer = new byte[512];

        public void ExecFrame(bool render)
        {
            if (MultiResHack > 0 && render)
                Array.Clear(FrameBuffer, 0, FrameBuffer.Length);
            
            while (true)
            {
                int ActiveDisplayStartLine = DisplayStartLine;
                int VBlankLine = ActiveDisplayStartLine + Registers[VDW] + 1;
                if (VBlankLine > 261)
                    VBlankLine = 261;
                ActiveLine = ScanLine - ActiveDisplayStartLine;
                bool InActiveDisplay = (ScanLine >= ActiveDisplayStartLine) && (ScanLine < VBlankLine);

                if (ScanLine == ActiveDisplayStartLine)
                    RCRCounter = 0x40;

                if (ScanLine == VBlankLine)
                    UpdateSpriteAttributeTable();

                if (RCRCounter == (Registers[RCR] & 0x3FF))
                {
                    if (RasterCompareInterruptEnabled)
                    {
                        StatusByte |= StatusRasterCompare;
                        cpu.IRQ1Assert = true;
                    }
                }

                cpu.Execute(HBlankCycles);

                if (InActiveDisplay)
                {
                    if (ScanLine == ActiveDisplayStartLine)
                        BackgroundY = Registers[BYR];
                    else
                    {
                        BackgroundY++;
                        BackgroundY &= 0x01FF;
                    }
                    if (render) RenderScanLine();
                }

                if (ScanLine == VBlankLine && VBlankInterruptEnabled)
                    StatusByte |= StatusVerticalBlanking;

                if (ScanLine == VBlankLine + 4 && SatDmaPerformed)
                {
                    SatDmaPerformed = false;
                    if ((Registers[DCR] & 1) > 0)
                        StatusByte |= StatusVramSatDmaComplete;
                }

                cpu.Execute(2);

                if ((StatusByte & (StatusVerticalBlanking | StatusVramSatDmaComplete)) != 0)
                    cpu.IRQ1Assert = true;

                cpu.Execute(455 - HBlankCycles - 2);

                if (InActiveDisplay == false && DmaRequested)
                    RunDmaForScanline();

                ScanLine++;
                RCRCounter++;

                if (ScanLine == vce.NumberOfScanlines)
                {
                    ScanLine = 0;
                    break;
                }
            }
        }

        public void RenderScanLine()
        {
            if (ActiveLine >= FrameHeight)
                return;

            RenderBackgroundScanline(pce.CoreComm.PCE_ShowBG1);
            RenderSpritesScanline(pce.CoreComm.PCE_ShowOBJ1);
        }

        void RenderBackgroundScanline(bool show)
        {
            Array.Clear(PriorityBuffer, 0, FrameWidth);

            if (BackgroundEnabled == false)
            {
                for (int i = 0; i < FrameWidth; i++)
                    FrameBuffer[(ActiveLine * FramePitch) + i] = vce.Palette[256];
                return;
            }

            int batHeight = BatHeight * 8;
            int batWidth = BatWidth * 8;

            int vertLine = BackgroundY;
            vertLine %= batHeight;
            int yTile = (vertLine / 8);
            int yOfs = vertLine % 8;

            // This is not optimized. But it seems likely to remain that way.
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
                    FrameBuffer[(ActiveLine * FramePitch) + x] = vce.Palette[0];
                else
                {
                    FrameBuffer[(ActiveLine * FramePitch) + x] = show ? vce.Palette[paletteBase + c] : vce.Palette[0];
                    PriorityBuffer[x] = 1;
                }
            }
        }

        byte[] heightTable = { 16, 32, 64, 64 };

        public void RenderSpritesScanline(bool show)
        {
            if (SpritesEnabled == false)
                return;

            Array.Clear(InterSpritePriorityBuffer, 0, FrameWidth);
            bool Sprite4ColorMode = Sprite4ColorModeEnabled;
            int activeSprites = 0;

            for (int i = 0; i < 64; i++)
            {
                if (activeSprites >= 16 && PerformSpriteLimit)
                    break;
                
                int y = (SpriteAttributeTable[(i * 4) + 0] & 1023) - 64;
                int x = (SpriteAttributeTable[(i * 4) + 1] & 1023) - 32;
                ushort flags = SpriteAttributeTable[(i * 4) + 3];
                int height = heightTable[(flags >> 12) & 3];
                int width = (flags & 0x100) == 0 ? 16 : 32;

                if (y + height <= ActiveLine || y > ActiveLine)
                    continue;

                activeSprites += width == 16 ? 1 : 2;
                
                int patternNo = (((SpriteAttributeTable[(i * 4) + 2]) >> 1) & 0x1FF);
                int paletteBase = 256 + ((flags & 15) * 16);
                bool priority = (flags & 0x80) != 0;
                bool hflip = (flags & 0x0800) != 0;
                bool vflip = (flags & 0x8000) != 0;

                int colorMask = 0xFF;
                if (Sprite4ColorMode)
                {
                    if ((SpriteAttributeTable[(i * 4) + 2] & 1) == 0)
                        colorMask = 0x03;
                    else
                        colorMask = 0x0C;
                }

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
                            byte pixel = (byte)(SpriteBuffer[(patternNo * 256) + (yofs * 16) + (xs - x)] & colorMask);
                            if (colorMask == 0x0C)
                                pixel >>= 2;
                            if (pixel != 0 && InterSpritePriorityBuffer[xs] == 0)
                            {
                                InterSpritePriorityBuffer[xs] = 1;
                                if ((priority || PriorityBuffer[xs] == 0) && show)
                                    FrameBuffer[(ActiveLine * FramePitch) + xs] = vce.Palette[paletteBase + pixel];
                            }
                        }
                    }
                    if (width == 32)
                    {
                        patternNo++;
                        x += 16;
                        for (int xs = x >= 0 ? x : 0; xs < x + 16 && xs >= 0 && xs < FrameWidth; xs++)
                        {
                            byte pixel = (byte)(SpriteBuffer[(patternNo * 256) + (yofs * 16) + (xs - x)] & colorMask);
                            if (colorMask == 0x0C)
                                pixel >>= 2;
                            if (pixel != 0 && InterSpritePriorityBuffer[xs] == 0)
                            {
                                InterSpritePriorityBuffer[xs] = 1;
                                if ((priority || PriorityBuffer[xs] == 0) && show)
                                    FrameBuffer[(ActiveLine * FramePitch) + xs] = vce.Palette[paletteBase + pixel];
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
                            byte pixel = (byte)(SpriteBuffer[(patternNo * 256) + (yofs * 16) + 15 - (xs - x)] & colorMask);
                            if (colorMask == 0x0C)
                                pixel >>= 2;
                            if (pixel != 0 && InterSpritePriorityBuffer[xs] == 0)
                            {
                                InterSpritePriorityBuffer[xs] = 1;
                                if ((priority || PriorityBuffer[xs] == 0) && show)
                                    FrameBuffer[(ActiveLine * FramePitch) + xs] = vce.Palette[paletteBase + pixel];
                            }
                        }
                        if (width == 32)
                        {
                            patternNo--;
                            x += 16;
                            for (int xs = x >= 0 ? x : 0; xs < x + 16 && xs >= 0 && xs < FrameWidth; xs++)
                            {
                                byte pixel = (byte)(SpriteBuffer[(patternNo * 256) + (yofs * 16) + 15 - (xs - x)] & colorMask);
                                if (colorMask == 0x0C)
                                    pixel >>= 2;
                                if (pixel != 0 && InterSpritePriorityBuffer[xs] == 0)
                                {
                                    InterSpritePriorityBuffer[xs] = 1;
                                    if ((priority || PriorityBuffer[xs] == 0) && show)
                                        FrameBuffer[(ActiveLine * FramePitch) + xs] = vce.Palette[paletteBase + pixel];
                                }
                            }
                        }
                    }
                }
            }
        }

        int FramePitch = 256;
        int FrameWidth = 256;
        int FrameHeight = 240;
        int[] FrameBuffer = new int[256 * 240];

        public int[] GetVideoBuffer() { return FrameBuffer; }

        public int VirtualWidth    { get { return FramePitch; } }
        public int BufferWidth     { get { return FramePitch; } }
        public int BufferHeight    { get { return FrameHeight; } }
        public int BackgroundColor { get { return vce.Palette[256]; } }
    }
}
