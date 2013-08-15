using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental
{
    public abstract partial class VIC
    {
        public Func<int> InputAddress;
        public Func<bool> InputChipSelect;
        public Func<int> InputData;

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

        public VIC()
        {
            backgroundColor = new int[4];
            spriteMultiColor = new int[2];
            sprites = new Sprite[8];
            for (int i = 0; i < 8; i++)
                sprites[i] = new Sprite();
        }

        /// <summary>
        /// Desired 14-bit address from the VIC.
        /// </summary>
        public int OutputAddress()
        {
            return ADDR;
        }

        /// <summary>
        /// AEC pin output.
        /// </summary>
        public bool OutputAEC()
        {
            return AEC;
        }

        /// <summary>
        /// BA pin output.
        /// </summary>
        public bool OutputBA()
        {
            return BA;
        }

        /// <summary>
        /// CAS pin output.
        /// </summary>
        public bool OutputCAS()
        {
            return CAS;
        }

        /// <summary>
        /// 12-bit data output from the VIC.
        /// </summary>
        public int OutputData()
        {
            return DATA;
        }

        /// <summary>
        /// IRQ pin output.
        /// </summary>
        public bool OutputInterrupt()
        {
            return IRQ;
        }

        /// <summary>
        /// PHI0 pin output.
        /// </summary>
        public bool OutputPHI0()
        {
            return PHI0;
        }

        /// <summary>
        /// RAS pin output.
        /// </summary>
        public bool OutputRAS()
        {
            return RAS;
        }
    }
}
