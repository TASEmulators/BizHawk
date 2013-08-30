using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    sealed public partial class Vic
    {
        const int GRAPHICS_DATA_OUTPUT_MASK = 0xC000;
        const int GRAPHICS_DATA_INPUT_SHIFT = 16;

        enum GraphicsMode
        {
            Mode000,
            Mode001,
            Mode010,
            Mode011,
            Mode100,
            Mode101,
            Mode110,
            Mode111
        }

        int g_BufferC;
        int g_BufferG;
        int g_DataC;
        int g_DataG;
        int g_FillRasterX;
        GraphicsMode g_Mode;
        int g_OutData;
        int g_OutPixel;
        int g_ShiftRegister;

        void RenderG()
        {
            if ((rasterX & 0x7) == g_FillRasterX)
            {
                g_DataC = g_BufferC;

                if (multiColorMode && (bitmapMode || (g_DataC & 0x8) != 0))
                {
                    // load multicolor bits
                    g_ShiftRegister =
                        ((g_DataG & 0x03) << 0) |
                        ((g_DataG & 0x0C) << 2) |
                        ((g_DataG & 0x30) << 4) |
                        ((g_DataG & 0xC0) << 6)
                        ;

                    // duplicate bits
                    g_ShiftRegister |= g_ShiftRegister << 2;
                }
                else
                {
                    // load single color bits
                    g_ShiftRegister =
                        ((g_DataG & 0x01) << 1) |
                        ((g_DataG & 0x02) << 2) |
                        ((g_DataG & 0x04) << 3) |
                        ((g_DataG & 0x08) << 4) |
                        ((g_DataG & 0x10) << 5) |
                        ((g_DataG & 0x20) << 6) |
                        ((g_DataG & 0x40) << 7) |
                        ((g_DataG & 0x80) << 8)
                        ;
                }
            }

            switch (g_Mode)
            {
                default:

                    break;
                case GraphicsMode.Mode001:
                    break;
                case GraphicsMode.Mode010:
                    break;
                case GraphicsMode.Mode011:
                    break;
                case GraphicsMode.Mode100:
                    break;
                case GraphicsMode.Mode101:
                    break;
                case GraphicsMode.Mode110:
                    break;
                case GraphicsMode.Mode111:
                    break;
            }
        }

        void UpdateGraphicsMode()
        {
            if (!extraColorMode && !bitmapMode && !multiColorMode)
                g_Mode = GraphicsMode.Mode000;
            else if (!extraColorMode && !bitmapMode && multiColorMode)
                g_Mode = GraphicsMode.Mode001;
            else if (!extraColorMode && bitmapMode && !multiColorMode)
                g_Mode = GraphicsMode.Mode010;
            else if (!extraColorMode && bitmapMode && multiColorMode)
                g_Mode = GraphicsMode.Mode011;
            else if (extraColorMode && !bitmapMode && !multiColorMode)
                g_Mode = GraphicsMode.Mode100;
            else if (extraColorMode && !bitmapMode && multiColorMode)
                g_Mode = GraphicsMode.Mode101;
            else if (extraColorMode && bitmapMode && !multiColorMode)
                g_Mode = GraphicsMode.Mode110;
            else if (extraColorMode && bitmapMode && multiColorMode)
                g_Mode = GraphicsMode.Mode111;
        }
    }
}
