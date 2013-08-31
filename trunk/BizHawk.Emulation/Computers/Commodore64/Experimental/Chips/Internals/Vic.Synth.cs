using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    sealed public partial class Vic
    {
        int v_Pixel;

        public void Render()
        {
            RenderGraphics();
            RenderSprites();

            if (s_OutData && (!s_Priority || g_OutData < GRAPHICS_DATA_10))
            {
                if (s_Priority && g_OutData < GRAPHICS_DATA_10)
                    v_Pixel = s_OutPixel;
                else
                    v_Pixel = g_OutPixel;
            }
            else
            {
                v_Pixel = g_OutPixel;
            }
        }
    }
}
