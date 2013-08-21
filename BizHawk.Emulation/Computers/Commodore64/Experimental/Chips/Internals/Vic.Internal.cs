using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    sealed public partial class Vic
    {
        int address;
        bool aec;
        int aecCounter;
        bool ba;
        bool badLineCondition;
        bool badLineEnable;
        int characterData;
        int colorData;
        int data;
        int graphicsData;
        bool mainBorder;
        bool phi0;
        int phi1Data;
        int pixel;
        int rasterX;
        int refreshCounter;
        int rowCounter;
        bool verticalBorder;
        int videoCounter;
        int videoCounterBase;
        int videoMatrixLineIndex;

        public Vic(VicSettings settings)
        {
            // initialize timing values
            InitTiming(settings.timing);

            // calculate visible screen dimensions
            screenWidth = screenXEnd - screenXStart;
            screenHeight = screenYEnd - screenYStart;
            if (screenXEnd < screenXStart)
                screenWidth += rasterWidth;
            if (screenYEnd < screenYStart)
                screenHeight += rasterCount;

            // reset registers
            pixelBufferLength = 12;
            Reset();
        }

        public void Clock()
        {
            do
            {
                // process horizontal triggers
                if (rasterX == screenXStart)
                    hBlank = false;
                else if (rasterX == screenXEnd)
                {
                    hBlank = true;
                    rasterDelay = hBlankDelay;
                }
                if (rasterX == characterBAStart)
                    characterBA = false;
                else if (rasterX == characterBAEnd)
                {
                    characterBA = true;
                    graphicsFetch = false;
                }
                if (rasterX == characterFetchStart)
                {
                    graphicsFetch = true;
                    refreshFetch = false;
                }
                if (rasterX == rasterWidth)
                    rasterX = 0;
                else if (rasterX == rasterAdvance)
                {
                    // process vertical triggers
                    rasterY++;
                    if (rasterY == screenYStart)
                        vBlank = false;
                    else if (rasterY == screenYEnd)
                        vBlank = true;
                }

                // None is used for when we don't assert control.
                fetchState = phi0 ? FetchState.Idle : FetchState.None;

                // determine BA state
                ba = true;

                if (characterBA)
                {
                    // covers badlines and display area fetches
                    characterFetch = (badLineCondition && badLineEnable);
                    ba = !characterFetch;
                    fetchState = phi0 ? FetchState.Graphics : (characterFetch ? FetchState.Character : FetchState.None);
                }
                else if (refreshFetch)
                {
                    // covers memory refresh fetches
                    fetchState = phi0 ? FetchState.Refresh : FetchState.None;
                }
                else
                {
                    // covers sprite pointer and data fetches
                    foreach (Sprite sprite in sprites)
                    {
                        if (rasterX == sprite.BAStart)
                            sprite.BA = false;
                        else if (rasterX == sprite.BAEnd)
                        {
                            sprite.BA = true;
                            sprite.Fetch = false;
                        }
                        if (!sprite.BA && sprite.Enabled)
                        {
                            fetchState = FetchState.Sprite;
                            ba = false;
                            break;
                        }
                        if (rasterX == sprite.FetchStart)
                        {
                            sprite.Fetch = true;
                            fetchState = FetchState.Pointer;
                            break;
                        }
                    }
                }

                // determine AEC state
                if (ba)
                {
                    aecCounter = 7;
                    aec = true;
                }
                else
                {
                    if (aecCounter > 0)
                        aecCounter--;
                    else
                        aec = false;
                }


                // VIC can perform a fetch every half-cycle
                switch (fetchState)
                {
                    case FetchState.Character:
                        address = videoCounter | videoMemory;
                        colorData = ReadColorRam(address);
                        characterData = ReadRam(address);
                        data = characterData;
                        break;
                    case FetchState.Graphics:
                        address = (extraColorMode ? 0x39FF : 0x3FFF);
                        if (bitmapMode)
                            address &= (rowCounter | (videoCounter << 3) | (characterBitmap & 0x2000));
                        else
                            address &= (rowCounter | (data << 3) | characterBitmap);
                        data = ReadRam(address);
                        graphicsData = data;
                        break;
                    case FetchState.Idle:
                        address = (extraColorMode ? 0x39FF : 0x3FFF);
                        data = ReadRam(address);
                        break;
                    case FetchState.Pointer:
                        address = spriteIndex | videoMemory | 0x03F8;
                        data = ReadRam(address);
                        break;
                    case FetchState.Refresh:
                        address = refreshCounter | 0x3F00;
                        data = ReadRam(address);
                        break;
                    case FetchState.Sprite:
                        address = data | sprites[spriteIndex].Counter;
                        data = ReadRam(address);
                        break;
                }

                // render 4 pixels
                for (int i = 0; i < 4; i++)
                {
                    if (!hBlank && !vBlank)
                    {
                        videoBufferIndex++;
                    }
                    if (rasterDelay > 0)
                        rasterDelay--;
                    else
                        rasterX++;
                }

                phi0 = !phi0;
            } while (phi0);


            // at the end, clock other devices if applicable
            ClockPhi0();
        }

        public void Reset()
        {
            backgroundColor = new int[4];
            spriteMultiColor = new int[2];
            sprites = new Sprite[8];
            for (int i = 0; i < 8; i++)
                sprites[i] = new Sprite();
            for (int i = 0; i < 0x40; i++)
                Poke(i, 0);
            phi0 = false;

            // we set these so no video is displayed before
            // the first frame starts
            vBlank = true;
            hBlank = true;

            // empty out the pixel buffer
            pixelBuffer = new int[pixelBufferLength];
            pixelBufferIndex = 0;
            borderPixelBufferIndex = 8;
        }
    }
}
