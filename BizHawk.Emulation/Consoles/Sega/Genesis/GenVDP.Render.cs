using System;

namespace BizHawk.Emulation.Consoles.Sega
{
    public partial class GenVDP
    {
        public void RenderLine()
        {
            if (ScanLine == 0)
            {
                Array.Clear(FrameBuffer, 0, FrameBuffer.Length);

                //RenderPatterns();
                RenderPalette();
                RenderScrollA();
                RenderScrollB();
            }
            RenderSprites();
        }

        void RenderPalette()
        {
            for (int p = 0; p < 4; p++)
                for (int i = 0; i < 16; i++)
                    FrameBuffer[(p*FrameWidth) + i] = Palette[(p*16) + i];
        }

        void RenderPatterns()
        {
            for (int yi=0; yi<28; yi++)
                for (int xi=0; xi<(Display40Mode?40:32); xi++)
                    RenderPattern(xi * 8, yi * 8, (yi * (Display40Mode ? 40 : 32)) + xi, 0);
        }

        void RenderPattern(int x, int y, int pattern, int palette)
        {
            for (int yi = 0; yi < 8; yi++)
            {
                for (int xi = 0; xi < 8; xi++)
                {
                    byte c = PatternBuffer[(pattern*64) + (yi*8) + xi];
                    if (c != 0)
                        FrameBuffer[((y + yi)*FrameWidth) + xi + x] = Palette[(palette*16) + c];
                }
            }
        }

        void RenderScrollA()
        {
            for (int yc=0; yc<24; yc++)
            {
                for (int xc=0; xc<32; xc++)
                {
                    int cellOfs = NameTableAddrA + (yc*NameTableWidth*2) + (xc*2);
                    int info = (VRAM[cellOfs+1] << 8) | VRAM[cellOfs];
                    int pattern = info & 0x7FF;
                    int palette = (info >> 13) & 3;
                    RenderPattern(xc*8, yc*8, pattern,palette);
                }
            }
        }

        void RenderScrollB()
        {
            for (int yc = 0; yc < 24; yc++)
            {
                for (int xc = 0; xc < 40; xc++)
                {
                    int cellOfs = NameTableAddrB + (yc * NameTableWidth * 2) + (xc * 2);
                    int info = (VRAM[cellOfs+1] << 8) | VRAM[cellOfs];
                    int pattern = info & 0x7FF;
                    int palette = (info >> 13) & 3;
                    RenderPattern(xc * 8, yc * 8, pattern, palette);
                }
            }
        }

        static readonly int[] SpriteSizeTable = { 8, 16, 24, 32 };
        Sprite sprite;

        void RenderSprites()
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
                        if (sprite.X + xi < 0 || sprite.X + xi > FrameWidth)
                            continue;

                        int pixel = PatternBuffer[((pattern + ((xi / 8) * sprite.HeightCells)) * 64) + ((yline & 7) * 8) + (xi & 7)];
                        if (pixel != 0)
                            FrameBuffer[scanLineBase + sprite.X + xi] = Palette[paletteBase + pixel];
                    }
                } else { // HFlip
                    int pattern = sprite.PatternIndex + ((yline / 8)) + (sprite.HeightCells * (sprite.WidthCells - 1));

                    for (int xi = 0; xi < sprite.WidthPixels; xi++)
                    {
                        if (sprite.X + xi < 0 || sprite.X + xi > FrameWidth)
                            continue;

                        int pixel = PatternBuffer[((pattern + ((-xi / 8) * sprite.HeightCells)) * 64) + ((yline & 7) * 8) + (7 - (xi & 7))];
                        if (pixel != 0)
                            FrameBuffer[scanLineBase + sprite.X + xi] = Palette[paletteBase + pixel];
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
    }
}