using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    sealed public partial class Vic
    {
        public Vic(VicSettings settings)
        {
            backgroundColor = new int[4];
            sprites = new Sprite[8];
            frequency = 0;
            rasterCount = 0;
            rasterWidth = 0;
            rasterY = 0;
            screenHeight = 0;
            screenWidth = 0;
            spriteMultiColor = new int[2];
            videoBuffer = new int[screenHeight * screenWidth];
        }
    }
}
