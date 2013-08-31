using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    sealed public partial class Vic
    {
        const int GRAPHICS_DATA_00 = 0;
        const int GRAPHICS_DATA_01 = 0x4000;
        const int GRAPHICS_DATA_10 = GRAPHICS_DATA_01 << 1;
        const int GRAPHICS_DATA_11 = GRAPHICS_DATA_01 | GRAPHICS_DATA_10;
        const int GRAPHICS_DATA_OUTPUT_MASK = GRAPHICS_DATA_11;

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
        bool g_Idle;
        int g_FillRasterX;
        GraphicsMode g_Mode;
        int g_OutData;
        int g_OutPixel;
        int g_ShiftRegister;

        void RenderGraphics()
        {
            if ((rasterX & 0x7) == g_FillRasterX)
            {
                g_DataC = g_BufferC;

                if (multiColorMode && (bitmapMode || (g_DataC & 0x8) != 0))
                {
                    // load multicolor bits
                    // xx00xx11xx22xx33
                    g_ShiftRegister =
                        ((g_DataG & 0x03) << 0) |
                        ((g_DataG & 0x0C) << 2) |
                        ((g_DataG & 0x30) << 4) |
                        ((g_DataG & 0xC0) << 6)
                        ;

                    // duplicate bits
                    // 0000111122223333
                    g_ShiftRegister |= g_ShiftRegister << 2;
                }
                else
                {
                    // load single color bits
                    // 0x1x2x3x4x5x6x7x
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

                    if (!bitmapMode)
                    {
                        // duplicate bits
                        // 0011223344556677
                        g_ShiftRegister |= g_ShiftRegister << 1;
                    }
                    else
                    {
                        // convert to bitmap format
                        g_ShiftRegister = (g_ShiftRegister | 0x5555) ^ (g_ShiftRegister >> 1);
                    }
                }
            }

            g_OutData = g_ShiftRegister & GRAPHICS_DATA_OUTPUT_MASK;

            switch (g_Mode)
            {
                case GraphicsMode.Mode000:
                case GraphicsMode.Mode001:
                    if (g_OutData == GRAPHICS_DATA_00)
                        g_OutPixel = backgroundColor[0];
                    else if (g_OutData == GRAPHICS_DATA_11)
                        g_OutPixel = g_Idle ? 0 : ((g_DataC >> 8) & 0x7);
                    else if (g_OutData == GRAPHICS_DATA_01)
                        g_OutPixel = g_Idle ? 0 : backgroundColor[1];
                    else
                        g_OutPixel = g_Idle ? 0 : backgroundColor[2];
                    break;
                case GraphicsMode.Mode010:
                case GraphicsMode.Mode011:
                    if (g_OutData == GRAPHICS_DATA_00)
                        g_OutPixel = backgroundColor[0];
                    else if (g_OutData == GRAPHICS_DATA_01)
                        g_OutPixel = (g_DataC >> 4) & 0xF;
                    else if (g_OutData == GRAPHICS_DATA_10)
                        g_OutPixel = g_DataC & 0xF;
                    else
                        g_OutPixel = g_DataC >> 8;
                    break;
                case GraphicsMode.Mode100:
                    if (g_OutData == GRAPHICS_DATA_00)
                        g_OutPixel = backgroundColor[(g_DataC >> 6) & 0x3];
                    else
                        g_OutPixel = g_DataC >> 8;
                    break;
                default:
                    g_OutPixel = 0;
                    break;
            }

            g_ShiftRegister <<= 2;
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
