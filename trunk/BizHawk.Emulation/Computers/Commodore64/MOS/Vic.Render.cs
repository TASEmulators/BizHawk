using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
    sealed public partial class Vic
    {
        protected int ecmPixel;
        protected int pixel;
        protected int[] pixelBackgroundBuffer;
        protected int pixelBackgroundBufferDelay;
        protected int pixelBackgroundBufferIndex;
        protected int[] pixelBuffer;
        protected int pixelBufferDelay;
        protected int pixelBufferIndex;
        protected int pixelData;
        protected int pixelOwner;
        protected int sprData;
        protected int sprPixel;
        protected VicVideoMode videoMode;

        protected enum VicVideoMode : int
        {
            Mode000,
            Mode001,
            Mode010,
            Mode011,
            Mode100,
            ModeBad
        }

        private void Render()
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
                pixelOwner = 8;
                for (int j = 0; j < 8; j++)
                {
                    sprData = 0;
                    sprPixel = pixel;

                    Sprite spr = sprites[j];

                    if (spr.x == rasterX)
                        spr.shiftEnable = true;

                    if (spr.shiftEnable)
                    {
                        if (spr.multicolor)
                        {
                            sprData = (spr.sr & 0xC00000);
                            if (spr.multicolorCrunch && spr.xCrunch)
                                spr.sr <<= 2;
                            spr.multicolorCrunch ^= spr.xCrunch;
                        }
                        else
                        {
                            sprData = (spr.sr & 0x800000);
                            if (spr.xCrunch)
                                spr.sr <<= 1;
                        }
                        spr.xCrunch ^= spr.xExpand;

                        if (sprData != 0)
                        {
                            if (sprData == 0x400000)
                                sprPixel = spriteMulticolor0;
                            else if (sprData == 0x800000)
                                sprPixel = spr.color;
                            else if (sprData == 0xC00000)
                                sprPixel = spriteMulticolor1;

                            // sprite-sprite collision
                            if (pixelOwner >= 8)
                            {
                                if (!spr.priority || (pixelDataBuffer[pixelBackgroundBufferIndex] < 0x80))
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
                            if (!borderOnVertical && (pixelDataBuffer[pixelBackgroundBufferIndex] == 0x80))
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

                switch (videoMode)
                {
                    case VicVideoMode.Mode000:
                        pixelData = (sr & 0x80);
                        sr <<= 1;
                        pixel = (pixelData != 0) ? displayC >> 8 : backgroundColor0;
                        break;
                    case VicVideoMode.Mode001:
                        if ((displayC & 0x800) != 0)
                        {
                            // multicolor 001
                            pixelData = (sr & 0xC0);
                            if ((bitmapColumn & 1) != 0)
                                sr <<= 2;

                            if (pixelData == 0x00)
                                pixel = backgroundColor0;
                            else if (pixelData == 0x40)
                                pixel = backgroundColor1;
                            else if (pixelData == 0x80)
                                pixel = backgroundColor2;
                            else
                                pixel = (displayC & 0x700) >> 8;
                        }
                        else
                        {
                            // standard 001
                            pixelData = (sr & 0x80);
                            sr <<= 1;
                            pixel = (pixelData != 0) ? (displayC >> 8) : backgroundColor0;
                        }
                        break;
                    case VicVideoMode.Mode010:
                        pixelData = (sr & 0x80);
                        sr <<= 1;
                        pixel = (pixelData != 0) ? ((displayC >> 4) & 0xF) : (displayC & 0xF);
                        break;
                    case VicVideoMode.Mode011:
                        pixelData = (sr & 0xC0);
                        if ((bitmapColumn & 1) != 0)
                            sr <<= 2;

                        if (pixelData == 0x00)
                            pixel = backgroundColor0;
                        else if (pixelData == 0x40)
                            pixel = (displayC >> 4) & 0xF;
                        else if (pixelData == 0x80)
                            pixel = displayC & 0xF;
                        else
                            pixel = (displayC >> 8) & 0xF;
                        break;
                    case VicVideoMode.Mode100:
                        pixelData = (sr & 0x80);
                        sr <<= 1;
                        if (pixelData != 0)
                        {
                            pixel = displayC >> 8;
                        }
                        else
                        {
                            ecmPixel = (displayC) & 0xC0;
                            if (ecmPixel == 0x00)
                                pixel = backgroundColor0;
                            else if (ecmPixel == 0x40)
                                pixel = backgroundColor1;
                            else if (ecmPixel == 0x80)
                                pixel = backgroundColor2;
                            else
                                pixel = backgroundColor3;
                        }
                        break;
                    default:
                        pixelData = 0;
                        pixel = 0;
                        break;
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
