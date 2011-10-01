using System;

namespace BizHawk.Emulation.Consoles.Sega
{
    public partial class GenVDP
    {
        public void RenderLine()
        {
            if (ScanLine == 223)
            {
                for (int i = 0; i < FrameBuffer.Length; i++)
                    FrameBuffer[i] = 0;

                //RenderPatterns();
                RenderPalette();
                RenderScrollA();
                RenderScrollB();
                RenderSprites();
            }
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


        void RenderSprites()
        {
            Sprite sprite = FetchSprite(0);
            /*if (sprite.X > 0)
                Console.WriteLine("doot");*/
        }

        Sprite FetchSprite(int spriteNo)
        {
            int satbase = SpriteAttributeTableAddr + (spriteNo*8);
            Sprite sprite = new Sprite();
            sprite.Y = (VRAM[satbase + 1] | (VRAM[satbase + 0] << 8) & 0x3FF) - 128;
            sprite.X = (VRAM[satbase + 7] | (VRAM[satbase + 6] << 8) & 0x3FF) - 128;
            sprite.Width = ((VRAM[satbase + 2] >> 2) & 3) + 1;
            sprite.Height = (VRAM[satbase + 2] & 3) + 1;
            sprite.Link = VRAM[satbase + 3] & 0x7F;
            sprite.PatternIndex = VRAM[satbase + 5] | (VRAM[satbase + 6] << 8) & 0x7FF;
            sprite.Palette = (VRAM[satbase + 5] >> 5) & 3;
            return sprite;
        }

     
        struct Sprite
        {
            public int X, Y;
            public int Width, Height;
            public int Link;
            public int Palette;
            public int PatternIndex;
        }
    }
}