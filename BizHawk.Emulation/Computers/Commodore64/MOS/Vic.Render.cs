using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
    public abstract partial class Vic
    {
        private int ecmPixel;
        private int pixel;
        private int[] pixelBackgroundBuffer;
        private int pixelBackgroundBufferDelay;
        private int pixelBackgroundBufferIndex;
        private int[] pixelBuffer;
        private int pixelBufferDelay;
        private int pixelBufferIndex;
        private int pixelData;

        private void Render()
        {

            {
                renderEnabled = bufRect.Contains(bufPoint);

                for (int i = 0; i < 4; i++)
                {
                    if (borderCheckLEnable && rasterX == borderL)
                    {
                        if (rasterLine == borderB)
                            borderOnVertical = true;
                        if (rasterLine == borderT && displayEnable)
                            borderOnVertical = false;
                        if (!borderOnVertical)
                            borderOnMain = false;
                    }
                    if (borderCheckREnable && rasterX == borderR)
                    {
                        borderOnMain = true;
                    }

                    // recall pixel from buffer
                    pixel = pixelBuffer[pixelBufferIndex];

                    // plot pixel if within viewing area
                    if (renderEnabled)
                    {
                        buf[bufOffset] = palette[pixel];
                        bufOffset++;
                        if (bufOffset == bufLength)
                            bufOffset = 0;
                    }
                    bufPoint.X++;
                    if (bufPoint.X == bufWidth)
                    {
                        bufPoint.X = 0;
                        bufPoint.Y++;
                        if (bufPoint.Y == bufHeight)
                            bufPoint.Y = 0;
                    }

                    // put the pixel from the background buffer into the main buffer
                    pixel = pixelBackgroundBuffer[pixelBackgroundBufferIndex];

                    // render sprite
                    int pixelOwner = 8;
                    for (int j = 0; j < 8; j++)
                    {
                        int sprData;
                        int sprPixel = pixel;

                        Sprite spr = sprites[j];

                        if (spr.x == rasterX)
                            spr.shiftEnable = true;

                        if (spr.shiftEnable)
                        {
                            if (spr.multicolor)
                            {
                                sprData = (spr.sr & 0xC00000) >> 22;
                                if (spr.multicolorCrunch && spr.xCrunch)
                                    spr.sr <<= 2;
                                spr.multicolorCrunch ^= spr.xCrunch;
                            }
                            else
                            {
                                sprData = (spr.sr & 0x800000) >> 22;
                                if (spr.xCrunch)
                                    spr.sr <<= 1;
                            }
                            spr.xCrunch ^= spr.xExpand;

                            if (sprData == 1) 
                                sprPixel = spriteMulticolor0;
                            else if (sprData == 2) 
                                sprPixel = spr.color;
                            else if (sprData == 3) 
                                sprPixel = spriteMulticolor1;

                            if (sprData != 0)
                            {
                                // sprite-sprite collision
                                if (pixelOwner >= 8)
                                {
                                    if (!spr.priority || (pixelDataBuffer[pixelBackgroundBufferIndex] < 0x2))
                                        pixel = sprPixel;
                                    pixelOwner = j;
                                }
                                else
                                {
                                    if (!borderOnVertical)
                                    {
                                        spr.collideSprite = true;
                                        sprites[pixelOwner].collideSprite = true;
                                    }
                                }

                                // sprite-data collision
                                if (!borderOnVertical && (pixelDataBuffer[pixelBackgroundBufferIndex] >= 0x2))
                                {
                                    spr.collideData = true;
                                }
                            }
                            if (spr.sr == 0)
                                spr.shiftEnable = false; //optimization
                        }
                    }

                    // border doesn't work with the background buffer
                    if (borderOnMain || borderOnVertical)
                        pixel = borderColor;

                    // store pixel in buffer
                    pixelBuffer[pixelBufferIndex] = pixel;

                    // fill shift register
                    if (xOffset == xScroll)
                    {
                        if (displayIndex < 40 && !idle)
                        {
                            displayC = bufferC[displayIndex];
                            sr |= bufferG[displayIndex];
                        }
                        bitmapColumn = 0;
                    }

                    if (!extraColorMode && !bitmapMode & !multicolorMode)
                    {
                        // 000
                        pixelData = (sr & 0x80) >> 6;
                        sr <<= 1;
                        pixel = (pixelData != 0) ? displayC >> 8 : backgroundColor0;
                    }
                    else if (!extraColorMode && !bitmapMode & multicolorMode)
                    {
                        // 001
                        if ((displayC & 0x800) != 0)
                        {
                            // multicolor 001
                            pixelData = (sr & 0xC0) >> 6;
                            if ((bitmapColumn & 1) != 0)
                                sr <<= 2;

                            if (pixelData == 0)
                                pixel = backgroundColor0;
                            else if (pixelData == 1)
                                pixel = backgroundColor1;
                            else if (pixelData == 2)
                                pixel = backgroundColor2;
                            else
                                pixel = (displayC & 0x700) >> 8;
                        }
                        else
                        {
                            // standard 001
                            pixelData = (sr & 0x80) >> 6;
                            sr <<= 1;
                            pixel = (pixelData != 0) ? (displayC >> 8) : backgroundColor0;
                        }
                    }
                    else if (!extraColorMode && bitmapMode & !multicolorMode)
                    {
                        // 010
                        pixelData = (sr & 0x80) >> 6;
                        sr <<= 1;
                        pixel = (pixelData != 0) ? ((displayC >> 4) & 0xF) : (displayC & 0xF);
                    }
                    else if (!extraColorMode && bitmapMode & multicolorMode)
                    {
                        // 011
                        pixelData = (sr & 0xC0) >> 6;
                        if ((bitmapColumn & 1) != 0)
                            sr <<= 2;

                        if (pixelData == 0) 
                            pixel = backgroundColor0;
                        else if (pixelData == 1) 
                            pixel = (displayC >> 4) & 0xF;
                        else if (pixelData == 2) 
                            pixel = displayC & 0xF;
                        else 
                            pixel = (displayC >> 8) & 0xF;
                    }
                    else if (extraColorMode && !bitmapMode & !multicolorMode)
                    {
                        // 100
                        pixelData = (sr & 0x80) >> 6;
                        sr <<= 1;
                        if (pixelData != 0)
                        {
                            pixel = displayC >> 8;
                        }
                        else
                        {
                            ecmPixel = (displayC >> 6) & 0x3;
                            if (ecmPixel == 0)
                                pixel = backgroundColor0;
                            else if (ecmPixel == 1)
                                pixel = backgroundColor1;
                            else if (ecmPixel == 2)
                                pixel = backgroundColor2;
                            else
                                pixel = backgroundColor3;
                        }
                    }
                    else if (extraColorMode && !bitmapMode & multicolorMode)
                    {
                        // 101
                        pixelData = 0;
                        pixel = 0;
                    }
                    else if (extraColorMode && bitmapMode & !multicolorMode)
                    {
                        // 110
                        pixelData = 0;
                        pixel = 0;
                    }
                    else
                    {
                        // 111
                        pixelData = 0;
                        pixel = 0;
                    }

                    // put the rendered pixel into the background buffer
                    pixelDataBuffer[pixelBackgroundBufferIndex] = pixelData;
                    pixelBackgroundBuffer[pixelBackgroundBufferIndex] = pixel;
                    pixelBackgroundBufferIndex++;
                    if (pixelBackgroundBufferIndex == pixelBackgroundBufferDelay)
                        pixelBackgroundBufferIndex = 0;

                    // advance pixel buffer
                    pixelBufferIndex++;
                    if (pixelBufferIndex == pixelBufferDelay)
                        pixelBufferIndex = 0;

                    rasterX++;
                    xOffset++;
                    bitmapColumn++;
                }
            }
        }
    }
}
