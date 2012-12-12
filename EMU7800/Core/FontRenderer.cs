/*
 * FontRenderer
 *
 * A simple font renderer for displaying text during emulation.  Font data and
 * rendering algorithm courtesy of Bradford W. Mott's Stella source.
 *
 * Copyright © 2004 Mike Murphy
 *
 */

using System;

namespace EMU7800.Core
{
    /// <summary>
    /// A simple font renderer for displaying text during emulation.
    /// </summary>
    public class FontRenderer
    {
        static readonly uint[] AlphaFontData =
        {
            0x699f999, // A
            0xe99e99e, // B
            0x6988896, // C
            0xe99999e, // D
            0xf88e88f, // E
            0xf88e888, // F
            0x698b996, // G
            0x999f999, // H
            0x7222227, // I
            0x72222a4, // J
            0x9accaa9, // K
            0x888888f, // L
            0x9ff9999, // M
            0x9ddbb99, // N
            0x6999996, // O
            0xe99e888, // P
            0x69999b7, // Q
            0xe99ea99, // R
            0x6986196, // S
            0x7222222, // T
            0x9999996, // U
            0x9999966, // V
            0x9999ff9, // W
            0x99fff99, // X
            0x9996244, // Y
            0xf12488f  // Z
        };

        static readonly uint[] DigitFontData =
        {
            0x69bd996, // 0
            0x2622227, // 1
            0x691248f, // 2
            0x6916196, // 3
            0xaaaf222, // 4
            0xf88e11e, // 5
            0x698e996, // 6
            0xf112244, // 7
            0x6996996, // 8
            0x6997196  // 9
        };

        /// <summary>
        /// Draw specified text at specified position using the specified foreground and background colors.
        /// </summary>
        /// <param name="frameBuffer"></param>
        /// <param name="text"></param>
        /// <param name="xoffset"></param>
        /// <param name="yoffset"></param>
        /// <param name="fore"></param>
        /// <param name="back"></param>
        /// <exception cref="ArgumentNullException">text must be non-null.</exception>
        public void DrawText(FrameBuffer frameBuffer, string text, int xoffset, int yoffset, byte fore, byte back)
        {
            if (text == null)
                throw new ArgumentNullException("text");

            var textchars = text.ToUpper().ToCharArray();

            for (var i = 0; i < text.Length + 1; i++)
            {
                for (var j = 0; j < 9; j++)
                {
                    var pos = (j + yoffset) * frameBuffer.VisiblePitch + i * 5;
                    for (var k = 0; k < 5; k++)
                    {
                        while (pos >= frameBuffer.VideoBufferByteLength)
                        {
                            pos -= frameBuffer.VideoBufferByteLength;
                        }
                        while (pos < 0)
                        {
                            pos += frameBuffer.VideoBufferByteLength;
                        }
                        frameBuffer.VideoBuffer[pos >> BufferElement.SHIFT][pos++] = back;
                    }
                }
            }

            for (var i = 0; i < text.Length; i++)
            {
                var c = textchars[i];
                uint fdata;

                switch (c)
                {
                    case '/':
                    case '\\':
                        fdata = 0x0122448;
                        break;
                    case '(':
                        fdata = 0x2488842;
                        break;
                    case ')':
                        fdata = 0x4211124;
                        break;
                    case '.':
                        fdata = 0x0000066;
                        break;
                    case ':':
                        fdata = 0x0660660;
                        break;
                    case '-':
                        fdata = 0x0007000;
                        break;
                    default:
                        if (c >= 'A' && c <= 'Z')
                        {
                            fdata = AlphaFontData[c - 'A'];
                        }
                        else if (c >= '0' && c <= '9')
                        {
                            fdata = DigitFontData[c - '0'];
                        }
                        else
                        {
                            fdata = 0;
                        }
                        break;
                }

                var ypos = 8;
                for (var j = 0; j < 32; j++)
                {
                    var xpos = j & 3;
                    if (xpos == 0)
                    {
                        ypos--;
                    }

                    var pos = (ypos + yoffset) * frameBuffer.VisiblePitch + (4 - xpos) + xoffset;
                    while (pos >= frameBuffer.VideoBufferByteLength)
                    {
                        pos -= frameBuffer.VideoBufferByteLength;
                    }
                    while (pos < 0)
                    {
                        pos += frameBuffer.VideoBufferByteLength;
                    }
                    if (((fdata >> j) & 1) != 0)
                    {
                        frameBuffer.VideoBuffer[pos >> BufferElement.SHIFT][pos] = fore;
                    }
                }
                xoffset += 5;
            }
        }
    }
}
