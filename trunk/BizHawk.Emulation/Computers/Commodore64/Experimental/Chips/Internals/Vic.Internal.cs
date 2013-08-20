using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    sealed public partial class Vic
    {
        class Sprite
        {
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
        }

        bool aec;
        bool ba;
        int[] backgroundColor;
        bool bitmapMode;
        int borderColor;
        bool cas;
        int characterBitmap;
        bool columnSelect;
        int data;
        bool dataCollisionInterrupt;
        bool dataCollisionInterruptEnable;
        bool displayEnable;
        bool extraColorMode;
        bool irq;
        bool lightPenInterrupt;
        bool lightPenInterruptEnable;
        int lightPenX;
        int lightPenY;
        bool multiColorMode;
        bool rasterInterrupt;
        bool rasterInterruptEnable;
        int rasterX;
        int rasterY;
        bool reset;
        bool rowSelect;
        bool spriteCollisionInterrupt;
        bool spriteCollisionInterruptEnable;
        int[] spriteMultiColor;
        Sprite[] sprites;
        int videoMemory;
        int xScroll;
        int yScroll;

        bool badLineCondition;
        bool badLineEnable;
        bool idleState;
        int pixelTimer;
        int rowCounter;
        int videoCounter;
        int videoCounterBase;
        int videoMatrixLineIndex;

        public Vic(VicSettings settings)
        {
            Reset();
        }

        public void Clock()
        {

            // at the end, clock other devices if applicable
            if (pixelTimer == 0)
            {
                pixelTimer = 8;
                ClockPhi0();
            }
            pixelTimer--;
        }

        public void Reset()
        {
            backgroundColor = new int[4];
            spriteMultiColor = new int[2];
            sprites = new Sprite[8];
            for (int i = 0; i < 8; i++)
                sprites[i] = new Sprite();
        }
    }
}
