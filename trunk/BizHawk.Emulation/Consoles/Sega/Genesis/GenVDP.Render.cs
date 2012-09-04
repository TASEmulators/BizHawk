using System;

namespace BizHawk.Emulation.Consoles.Sega
{
    public partial class GenVDP
    {
        // Priority buffer contents have the following values:
        // 0 = Backdrop color
        // 1 = Plane B Low Priority
        // 2 = Plane A Low Priority
        // 4 = Plane B High Priority
        // 5 = Plane A High Priority
        // 9 = Sprite has been drawn

        byte[] PriorityBuffer = new byte[320];

        static readonly byte[] PalXlatTable = { 0, 0, 36, 36, 73, 73, 109, 109, 145, 145, 182, 182, 219, 219, 255, 255 };

        public void RenderLine()
        {
            if (DisplayEnabled)
            {
                Array.Clear(PriorityBuffer, 0, 320);
                
                // TODO: I would like to be able to render Scroll A before Scroll B, in order to minimize overdraw.
                // But at the moment it complicates priority stuff.

                RenderScrollB();
                RenderScrollA();
                
                RenderSpritesScanline();
            }
            else
            {
                // If display is disabled, fill in with background color.
                for (int i = 0; i < FrameWidth; i++)
                    FrameBuffer[(ScanLine * FrameWidth) + i] = BackgroundColor;
            }

            //if (ScanLine == 223) // shrug
            //    RenderPalette();
        }

        void RenderPalette()
        {
            for (int p = 0; p < 4; p++)
                for (int i = 0; i < 16; i++)
                    FrameBuffer[(p*FrameWidth) + i] = Palette[(p*16) + i];
        }

        void RenderScrollAScanline(int xScroll, int yScroll, int nameTableBase, int startPixel, int endPixel)
        {
            const int lowPriority = 2;
            const int highPriority = 5;
            int yTile = ((ScanLine + yScroll) / 8) % NameTableHeight;
            int nameTableWidth = NameTableWidth;
            if (nameTableBase == NameTableAddrWindow)
                nameTableWidth = Display40Mode ? 64 : 32;

            // this is hellllla slow. but not optimizing until we implement & understand
            // all scrolling modes, shadow & hilight, etc.
            // in thinking about this, you could convince me to optimize the PCE background renderer now.
            // Its way simple in comparison. But the PCE sprite renderer is way worse than gen.
            for (int x = startPixel; x < endPixel; x++)
            {
                int xTile = Math.Abs(((x + (1024-xScroll)) / 8) % nameTableWidth);
                int xOfs = Math.Abs((x + (1024-xScroll)) & 7);
                int yOfs = (ScanLine + yScroll) % 8;
                int cellOfs = nameTableBase + (yTile * nameTableWidth * 2) + (xTile * 2);
                int nameTableEntry = VRAM[cellOfs] | (VRAM[cellOfs+1] << 8);
                int patternNo = nameTableEntry & 0x7FF;
                bool hFlip = ((nameTableEntry >> 11) & 1) != 0;
                bool vFlip = ((nameTableEntry >> 12) & 1) != 0;
                bool priority = ((nameTableEntry >> 15) & 1) != 0;
                int palette = (nameTableEntry >> 13) & 3;

                if (priority && PriorityBuffer[x] >= highPriority) continue;
                if (!priority && PriorityBuffer[x] >= lowPriority) continue;

                if (vFlip) yOfs = 7 - yOfs;
                if (hFlip) xOfs = 7 - xOfs;
                
                int texel = PatternBuffer[(patternNo * 64) + (yOfs * 8) + (xOfs)];
                if (texel == 0) continue;
                int pixel = Palette[(palette * 16) + texel];
                FrameBuffer[(ScanLine * FrameWidth) + x] = pixel;
                PriorityBuffer[x] = (byte) (priority ? highPriority : lowPriority);
            }
        }

        void CalculateWindowScanlines(out int startScanline, out int endScanline)
        {
            int data = Registers[0x12];
            int windowVPosition = data & 31;
            bool fromTop = (data & 0x80) == 0;
            
            if (windowVPosition == 0)
            {
                startScanline = -1;
                endScanline = -1;
                return;
            }

            if (fromTop)
            {
                startScanline = 0;
                endScanline = (windowVPosition * 8);
            } else {
                startScanline = windowVPosition * 8;
                endScanline = FrameHeight;
            }
        }

        void CalculateWindowPosition(out int startPixel, out int endPixel)
        {
            int data = Registers[0x11];
            int windowHPosition = (data & 31) * 2; // Window H position is set in 2-cell increments
            bool fromLeft = (data & 0x80) == 0;
            
            if (windowHPosition == 0)
            {
                startPixel = -1;
                endPixel = -1;
                return;
            }

            if (fromLeft)
            {
                startPixel = 0;
                endPixel = (windowHPosition * 8);
                if (endPixel > FrameWidth) 
                    endPixel = FrameWidth;
            }
            else
            {
                startPixel = windowHPosition * 8;
                endPixel = FrameWidth;
                if (startPixel > FrameWidth)
                {
                    startPixel = -1;
                    endPixel = -1;
                }
            }
        }

        void RenderScrollA()
        {
            // Calculate scroll offsets

            int hscroll = CalcHScrollPlaneA(ScanLine);
            int vscroll = VSRAM[0] & 0x3FF;

            // Calculate window dimensions

            int startWindowScanline, endWindowScanline;
            int startWindowPixel, endWindowPixel;
            CalculateWindowScanlines(out startWindowScanline, out endWindowScanline);
            CalculateWindowPosition(out startWindowPixel, out endWindowPixel);

            // Render scanline

            if (ScanLine >= startWindowScanline && ScanLine < endWindowScanline)  // Window takes up whole scanline
            {
                RenderScrollAScanline(0, 0, NameTableAddrWindow, 0, FrameWidth);
            }
            else if (startWindowPixel != -1) // Window takes up partial scanline
            {
                if (startWindowPixel == 0) // Window grows from left side
                {
                    RenderScrollAScanline(0, 0, NameTableAddrWindow, 0, endWindowPixel);
                    RenderScrollAScanline(hscroll, vscroll, NameTableAddrA, endWindowPixel, FrameWidth);
                }
                else // Window grows from right side
                {
                    RenderScrollAScanline(hscroll, vscroll, NameTableAddrA, 0, startWindowPixel);
                    RenderScrollAScanline(0, 0, NameTableAddrWindow, startWindowPixel, FrameWidth);
                }
            } 
            else // No window
            {   
                RenderScrollAScanline(hscroll, vscroll, NameTableAddrA, 0, FrameWidth);
            }
        }

        void RenderScrollB()
        {
            int bgColor = BackgroundColor;
            int xScroll = CalcHScrollPlaneB(ScanLine);
            int yScroll = VSRAM[1] & 0x3FF;

            const int lowPriority = 1;
            const int highPriority = 4;

            int yTile = ((ScanLine + yScroll) / 8) % NameTableHeight;

            // this is hellllla slow. but not optimizing until we implement & understand
            // all scrolling modes, shadow & hilight, etc.
            // in thinking about this, you could convince me to optimize the PCE background renderer now.
            // Its way simple in comparison. But the PCE sprite renderer is way worse than gen.
            for (int x = 0; x < FrameWidth; x++)
            {
                int xTile = Math.Abs(((x + (1024 - xScroll)) / 8) % NameTableWidth);
                int xOfs = Math.Abs((x + (1024 - xScroll)) & 7);
                int yOfs = (ScanLine + yScroll) % 8;
                int cellOfs = NameTableAddrB + (yTile * NameTableWidth * 2) + (xTile * 2);
                int nameTableEntry = VRAM[cellOfs] | (VRAM[cellOfs + 1] << 8);
                int patternNo = nameTableEntry & 0x7FF;
                bool hFlip = ((nameTableEntry >> 11) & 1) != 0;
                bool vFlip = ((nameTableEntry >> 12) & 1) != 0;
                bool priority = ((nameTableEntry >> 15) & 1) != 0;
                int palette = (nameTableEntry >> 13) & 3;

                if (priority && PriorityBuffer[x] >= highPriority) continue;
                if (!priority && PriorityBuffer[x] >= lowPriority) continue;

                if (vFlip) yOfs = 7 - yOfs;
                if (hFlip) xOfs = 7 - xOfs;

                int texel = PatternBuffer[(patternNo * 64) + (yOfs * 8) + (xOfs)];
                int pixel = Palette[(palette * 16) + texel];
                if (texel != 0)
                {
                    FrameBuffer[(ScanLine * FrameWidth) + x] = pixel;
                    PriorityBuffer[x] = (byte)(priority ? highPriority : lowPriority);   
                }
                else
                {
                    FrameBuffer[(ScanLine * FrameWidth) + x] = bgColor;
                }
            }
        }

        static readonly int[] SpriteSizeTable = { 8, 16, 24, 32 };
        Sprite sprite;

        void RenderSpritesScanline()
        {
            int scanLineBase = ScanLine * FrameWidth;
            int processedSprites = 0;
            // This is incredibly unoptimized. TODO...

            FetchSprite(0);
            while (true)
            {
                if (sprite.Y > ScanLine || sprite.Y+sprite.HeightPixels <= ScanLine)
                    goto nextSprite;

                if (sprite.X == -127) // masking code is not super tested
                    break; // TODO does masking mode 2 really exist?

                if (sprite.X + sprite.WidthPixels <= 0)
                    goto nextSprite;

                if (sprite.HeightCells == 2)
                    sprite.HeightCells = 2;

                int yline = ScanLine - sprite.Y;
                if (sprite.VFlip)
                    yline = sprite.HeightPixels - 1 - yline;
                int paletteBase = sprite.Palette * 16;
                if (sprite.HFlip == false)
                {
                    int pattern = sprite.PatternIndex + ((yline / 8));

                    for (int xi = 0; xi < sprite.WidthPixels; xi++)
                    {
                        if (sprite.X + xi < 0 || sprite.X + xi >= FrameWidth)
                            continue;

                        if (sprite.Priority == false && PriorityBuffer[sprite.X + xi] >= 3) continue;
                        if (PriorityBuffer[sprite.X + xi] == 9) continue;

                        int pixel = PatternBuffer[((pattern + ((xi / 8) * sprite.HeightCells)) * 64) + ((yline & 7) * 8) + (xi & 7)];
                        if (pixel != 0)
                        {
                            FrameBuffer[scanLineBase + sprite.X + xi] = Palette[paletteBase + pixel];
                            PriorityBuffer[sprite.X + xi] = 9;
                        }
                    }
                } else { // HFlip
                    int pattern = sprite.PatternIndex + ((yline / 8)) + (sprite.HeightCells * (sprite.WidthCells - 1));

                    for (int xi = 0; xi < sprite.WidthPixels; xi++)
                    {
                        if (sprite.X + xi < 0 || sprite.X + xi >= FrameWidth)
                            continue;

                        if (sprite.Priority == false && PriorityBuffer[sprite.X + xi] >= 3) continue;
                        if (PriorityBuffer[sprite.X + xi] == 9) continue;

                        int pixel = PatternBuffer[((pattern + ((-xi / 8) * sprite.HeightCells)) * 64) + ((yline & 7) * 8) + (7 - (xi & 7))];
                        if (pixel != 0)
                        {
                            FrameBuffer[scanLineBase + sprite.X + xi] = Palette[paletteBase + pixel];
                            PriorityBuffer[sprite.X + xi] = 9;
                        }
                    }
                }

            nextSprite:
                if (sprite.Link == 0)
                    break;
                if (++processedSprites > 80)
                    break;
                FetchSprite(sprite.Link);
            }
        }

        void FetchSprite(int spriteNo)
        {
            // Note - X/Y coordinates are 10-bits (3FF) but must be masked to 9-bits (1FF)
            // In interlace mode this behavior should change

            int SatBase = SpriteAttributeTableAddr + (spriteNo*8);
            sprite.Y = (VRAM[SatBase + 0] | (VRAM[SatBase + 1] << 8) & 0x1FF) - 128;
            sprite.X = (VRAM[SatBase + 6] | (VRAM[SatBase + 7] << 8) & 0x1FF) - 128;
            sprite.WidthPixels = SpriteSizeTable[(VRAM[SatBase + 3] >> 2) & 3];
            sprite.HeightPixels = SpriteSizeTable[VRAM[SatBase + 3] & 3];
            sprite.WidthCells = ((VRAM[SatBase + 3] >> 2) & 3) + 1;
            sprite.HeightCells = (VRAM[SatBase + 3] & 3) + 1;
            sprite.Link = VRAM[SatBase + 2] & 0x7F;
            sprite.PatternIndex = (VRAM[SatBase + 4] | (VRAM[SatBase + 5] << 8)) & 0x7FF;
            sprite.HFlip = ((VRAM[SatBase + 5] >> 3) & 1) != 0;
            sprite.VFlip = ((VRAM[SatBase + 5] >> 4) & 1) != 0;
            sprite.Palette = (VRAM[SatBase + 5] >> 5) & 3;
            sprite.Priority = ((VRAM[SatBase + 5] >> 7) & 1) != 0;
        }

        struct Sprite
        {
            public int X, Y;
            public int WidthPixels, HeightPixels;
            public int WidthCells, HeightCells;
            public int Link;
            public int Palette;
            public int PatternIndex;
            public bool Priority;
            public bool HFlip;
            public bool VFlip;
        }

        int CalcHScrollPlaneA(int line)
        {
            int ofs = 0;
            switch (Registers[11] & 3)
            {
                case 0: ofs = HScrollTableAddr; break;
                case 1: ofs = HScrollTableAddr + ((line & 7) * 4); break;
                case 2: ofs = HScrollTableAddr + ((line & ~7) * 4); break;
                case 3: ofs = HScrollTableAddr + (line * 4); break;
            }

            int value = VRAM[ofs] | (VRAM[ofs + 1] << 8);
            return value & 0x3FF;
        }

        int CalcHScrollPlaneB(int line)
        {
            int ofs = 0;
            switch (Registers[11] & 3)
            {
                case 0: ofs = HScrollTableAddr; break;
                case 1: ofs = HScrollTableAddr + ((line & 7) * 4); break;
                case 2: ofs = HScrollTableAddr + ((line & ~7) * 4); break;
                case 3: ofs = HScrollTableAddr + (line * 4); break;
            }

            int value = VRAM[ofs + 2] | (VRAM[ofs + 3] << 8);
            return value & 0x3FF;
        }
    }
}