using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    sealed public partial class Vic
    {
        const int SPRITE_DATA_00 = 0;
        const int SPRITE_DATA_01 = 0x400000;
        const int SPRITE_DATA_10 = SPRITE_DATA_01 << 1;
        const int SPRITE_DATA_11 = SPRITE_DATA_01 | SPRITE_DATA_10;
        const int SPRITE_DATA_OUTPUT_MASK = SPRITE_DATA_11;
       
        sealed class Sprite
        {
            public bool CrunchMC;
            public bool CrunchX;
            public bool CrunchY;
            public bool Display;
            public int ShiftRegister;
            public bool ShiftRegisterEnable;

            public int Color;
            public bool DataCollision;
            public bool Enabled;
            public bool ExpandX;
            public bool ExpandY;
            public bool Multicolor;
            public bool Priority;
            public bool SpriteCollision;
            public int X;
            public int Y;

            public Sprite()
            {
            }

            public void Clock()
            {
            }

            public void LoadP(int value)
            {
            }

            public void LoadS(int value)
            {
            }
        }

        Sprite s_CollideSprite;
        int s_Data;
        bool s_OutData;
        int s_OutPixel;
        bool s_Priority;
        Sprite[] sprites;

        void RenderSprites()
        {
            s_OutData = false;
            s_CollideSprite = null;

            foreach (Sprite sprite in sprites)
            {
                if (sprite.Display && rasterX == sprite.X)
                    sprite.ShiftRegisterEnable = true;

                if (sprite.ShiftRegisterEnable)
                {
                    if (sprite.ShiftRegister == 0)
                    {
                        sprite.ShiftRegisterEnable = false;
                        sprite.CrunchMC = true;
                        sprite.CrunchX = true;
                    }
                    else
                    {
                        sprite.CrunchX = !sprite.CrunchX || !sprite.ExpandX;
                        if (sprite.CrunchX)
                            sprite.CrunchMC = !sprite.CrunchMC || !sprite.Multicolor;

                        if (sprite.Multicolor)
                            s_Data = sprite.ShiftRegister & SPRITE_DATA_11;
                        else
                            s_Data = (sprite.ShiftRegister << 1) & SPRITE_DATA_10;

                        if (s_CollideSprite == null)
                        {
                            if (s_Data == SPRITE_DATA_10)
                                s_OutPixel = sprite.Color;
                            else if (s_Data == SPRITE_DATA_01)
                                s_OutPixel = spriteMultiColor[0];
                            else if (s_Data == SPRITE_DATA_11)
                                s_OutPixel = spriteMultiColor[1];

                            if (s_Data != SPRITE_DATA_00)
                            {
                                s_CollideSprite = sprite;
                                s_OutData = true;
                                s_Priority = sprite.Priority;
                            }
                        }
                        else if (s_Data != SPRITE_DATA_00)
                        {
                            s_CollideSprite.SpriteCollision = true;
                            sprite.SpriteCollision = true;
                            spriteCollisionInterrupt = true;
                        }

                        if (s_Data != SPRITE_DATA_00 && g_OutData >= GRAPHICS_DATA_10)
                        {
                            sprite.DataCollision = true;
                            dataCollisionInterrupt = true;
                        }

                        if (sprite.CrunchMC && sprite.CrunchX)
                            sprite.ShiftRegister <<= sprite.Multicolor ? 2 : 1;
                    }
                }
            }
        }
    }
}
