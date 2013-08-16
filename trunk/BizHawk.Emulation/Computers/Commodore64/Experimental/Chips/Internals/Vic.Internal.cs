using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    public partial class Vic
    {
        int bufferADDR;
        bool bufferAEC;
        bool bufferBA;
        bool bufferCAS;
        int bufferDATA;
        bool bufferIRQ;
        bool bufferPHI0;
        bool bufferRAS;

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

        int[] backgroundColor;
        bool bitmapMode;
        int borderColor;
        int characterBitmap;
        bool columnSelect;
        bool dataCollisionInterrupt;
        bool displayEnable;
        bool extraColorMode;
        byte interruptEnableRegister;
        bool lightPenInterrupt;
        int lightPenX;
        int lightPenY;
        bool multiColorMode;
        bool rasterInterrupt;
        int rasterX;
        int rasterY;
        bool reset;
        bool rowSelect;
        bool spriteCollisionInterrupt;
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

        public void Execute()
        {
            if (pixelTimer == 0)
            {
                bufferPHI0 = !bufferPHI0;
                pixelTimer = 8;

                badLineEnable |= (rasterY == 0x30 && displayEnable);
                if (!bufferPHI0)
                {
                    badLineCondition = (
                        badLineEnable && 
                        rasterY >= 0x030 && 
                        rasterY <= 0x0F7 && 
                        (rasterY & 0x007) == yScroll
                        );
                    if (!idleState && badLineCondition)
                        idleState = true;
                }
            }
            pixelTimer--;

        }
    }
}
