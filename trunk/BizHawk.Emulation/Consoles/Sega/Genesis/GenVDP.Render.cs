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

        // TODO, should provide startup register values.
        public void RenderLine()
        {
            Array.Clear(PriorityBuffer, 0, 320);
            int bgcolor = BackgroundColor;
            for (int ofs = ScanLine * FrameWidth, i = 0; i < FrameWidth; i++, ofs++)
                FrameBuffer[ofs] = bgcolor;

            if (DisplayEnabled)
            {
                RenderScrollA();
                RenderScrollB();
                RenderSpritesScanline();
            }

            //if (ScanLine == 223) // shrug
                //RenderPalette();
        }

        void RenderPalette()
        {
            for (int p = 0; p < 4; p++)
                for (int i = 0; i < 16; i++)
                    FrameBuffer[(p*FrameWidth) + i] = Palette[(p*16) + i];
        }

        void RenderBackgroundScanline(int xScroll, int yScroll, int nameTableBase, int lowPriority, int highPriority)
        {
            int yTile = ((ScanLine + yScroll) / 8) % NameTableHeight;

            // this is hellllla slow. but not optimizing until we implement & understand
            // all scrolling modes, shadow & hilight, etc.
            // in thinking about this, you could convince me to optimize the PCE background renderer now.
            // Its way simple in comparison. But the PCE sprite renderer is way worse than gen.
            for (int x = 0; x < FrameWidth; x++)
            {
                int xTile = Math.Abs(((x + (1024-xScroll)) / 8) % NameTableWidth);
                int xOfs = Math.Abs((x + (1024-xScroll)) & 7);
                int yOfs = (ScanLine + yScroll) % 8;
                int cellOfs = nameTableBase + (yTile * NameTableWidth * 2) + (xTile * 2);
                int nameTableEntry = VRAM[cellOfs] | (VRAM[cellOfs+1] << 8);
                int patternNo = nameTableEntry & 0x7FF;
                bool hFlip = ((nameTableEntry >> 11) & 1) != 0;
                bool vFlip = ((nameTableEntry >> 12) & 1) != 0;
                bool priority = ((nameTableEntry >> 15) & 1) != 0;
                int palette = (nameTableEntry >> 13) & 3;

                if (priority && PriorityBuffer[x] >= highPriority) continue;
                if (PriorityBuffer[x] >= lowPriority) continue;

                if (vFlip) yOfs = 7 - yOfs;
                if (hFlip) xOfs = 7 - xOfs;
                
                int texel = PatternBuffer[(patternNo * 64) + (yOfs * 8) + (xOfs)];
                if (texel == 0) continue;
                int pixel = Palette[(palette * 16) + texel];
                FrameBuffer[(ScanLine * FrameWidth) + x] = pixel;
                PriorityBuffer[x] = (byte) (priority ? highPriority : lowPriority);
            }
        }

        void RenderScrollA()
        {
            // todo scroll values
            int hscroll = CalcHScrollPlaneA(ScanLine);
            int vscroll = VSRAM[0] & 0x3FF;
            RenderBackgroundScanline(hscroll, vscroll, NameTableAddrA, 2, 5);
        }

        void RenderScrollB()
        {
            int hscroll = CalcHScrollPlaneB(ScanLine);
            int vscroll = VSRAM[1] & 0x3FF;
            RenderBackgroundScanline(hscroll, vscroll, NameTableAddrB, 1, 4);
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
                if (sprite.X + sprite.WidthPixels <= 0) 
                    goto nextSprite;
                if (sprite.X == -128)
                    throw new Exception("bleeeh"); // masking code is not really tested
                    //break; // TODO this satisfies masking mode 1 but not masking mode 2

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
            int satbase = SpriteAttributeTableAddr + (spriteNo*8);
            sprite.Y = (VRAM[satbase + 0] | (VRAM[satbase + 1] << 8) & 0x3FF) - 128;
            sprite.X = (VRAM[satbase + 6] | (VRAM[satbase + 7] << 8) & 0x3FF) - 128;
            sprite.WidthPixels = SpriteSizeTable[(VRAM[satbase + 3] >> 2) & 3];
            sprite.HeightPixels = SpriteSizeTable[VRAM[satbase + 3] & 3];
            sprite.WidthCells = ((VRAM[satbase + 3] >> 2) & 3) + 1;
            sprite.HeightCells = (VRAM[satbase + 3] & 3) + 1;
            sprite.Link = VRAM[satbase + 2] & 0x7F;
            sprite.PatternIndex = (VRAM[satbase + 4] | (VRAM[satbase + 5] << 8)) & 0x7FF;
            sprite.HFlip = ((VRAM[satbase + 5] >> 3) & 1) != 0;
            sprite.VFlip = ((VRAM[satbase + 5] >> 4) & 1) != 0;
            sprite.Palette = (VRAM[satbase + 5] >> 5) & 3;
            sprite.Priority = ((VRAM[satbase + 5] >> 7) & 1) != 0;
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