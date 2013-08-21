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
        int[] characterMatrix;
        int colorData;
        int[] colorMatrix;
        int data;
        int graphicsData;
        int graphicsGeneratorCharacter;
        int graphicsGeneratorColor;
        int graphicsGeneratorData;
        int graphicsGeneratorPixel;
        int graphicsGeneratorPixelData;
        bool idleState;
        bool mainBorder;
        int mainBorderEnd;
        int mainBorderStart;
        bool phi0;
        int phi1Data;
        int pixel;
        int[] pixelDataBuffer;
        int rasterX;
        int refreshCounter;
        int rowCounter;
        bool verticalBorder;
        int verticalBorderEnd;
        int verticalBorderStart;
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

            // create video buffer
            videoBuffer = new int[screenWidth * screenHeight];

            // reset registers
            pixelBufferLength = 12;
            Reset();
        }

        public void Clock()
        {
            // these should be cached somewhere
            mainBorderStart = columnSelect ? 0x18 : 0x1F;
            mainBorderEnd = columnSelect ? 0x158 : 0x14F;
            verticalBorderStart = rowSelect ? 0x33 : 0x37;
            verticalBorderEnd = rowSelect ? 0xFB : 0xF7;

            do
            {
                // process hblank trigger
                if (rasterX == screenXStart)
                    hBlank = false;
                else if (rasterX == screenXEnd && !hBlank)
                {
                    hBlank = true;
                    rasterDelay = hBlankDelay;

                    // process row counter
                    if (rowCounter == 7)
                    {
                        idleState = true;
                        videoCounterBase = videoCounter;
                    }
                    else if (!idleState)
                        rowCounter++;

                    // process vertical border flipflop
                    if (rasterY == mainBorderEnd)
                        verticalBorder = true;
                    if (rasterY == mainBorderStart && displayEnable)
                        verticalBorder = false;
                }

                // process character BA trigger
                if (rasterX == characterBAStart)
                    characterBA = false;
                else if (rasterX == characterBAEnd)
                {
                    characterBA = true;
                    graphicsFetch = false;
                }

                // process character fetch trigger
                if (rasterX == characterFetchStart)
                {
                    graphicsFetch = true;
                    refreshFetch = false;
                }

                // process new line/raster triggers
                if (rasterX == rasterWidth)
                {
                    rasterX = 0;
                    videoCounter = videoCounterBase;
                    videoMatrixLineIndex = 0;
                }
                else if (rasterX == rasterAdvance)
                {
                    // process vertical triggers
                    rasterY++;
                    if (rasterY == screenYStart)
                        vBlank = false;
                    else if (rasterY == screenYEnd)
                        vBlank = true;
                    else if (rasterY == rasterCount)
                    {
                        rasterY = 0;
                        videoCounterBase = 0;
                        videoBufferIndex = 0;
                    }
                }

                // determine BA and fetch state
                ba = true;

                // None is used for when we don't assert control.
                fetchState = phi0 ? FetchState.Idle : FetchState.None;

                if (characterBA)
                {
                    // covers badlines and display area fetches
                    characterFetch = (badLineCondition && badLineEnable);
                    ba = !characterFetch;
                    fetchState = phi0 ? FetchState.Graphics : FetchState.Character;
                }
                else if (refreshFetch)
                {
                    // covers memory refresh fetches
                    fetchState = phi0 ? FetchState.Refresh : FetchState.None;
                }
                else
                {
                    // covers sprite pointer and data fetches
                    spriteIndex = 0;
                    foreach (Sprite sprite in sprites)
                    {
                        if (rasterX == sprite.BAStart)
                            sprite.BA = false;
                        else if (rasterX == sprite.BAEnd)
                        {
                            sprite.BA = true;
                            sprite.Fetch = false;
                        }
                        if (sprite.Fetch)
                        {
                            fetchState = FetchState.Sprite;
                            ba = false;
                            break;
                        }
                        else if (rasterX == sprite.FetchStart)
                        {
                            sprite.Fetch = sprite.Enabled;
                            fetchState = FetchState.Pointer;
                            ba = !sprite.Fetch;
                            break;
                        }
                        spriteIndex++;
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
                        if (badLineCondition)
                        {
                            address = videoCounter | videoMemory;
                            colorMatrix[videoMatrixLineIndex] = colorData = ReadColorRam(address);
                            characterMatrix[videoMatrixLineIndex] = characterData = data = ReadRam(address);
                        }
                        else if (!idleState)
                        {
                            colorData = colorMatrix[videoMatrixLineIndex];
                            characterData = characterMatrix[videoMatrixLineIndex];
                        }
                        else
                        {
                            colorData = 0;
                            characterData = 0;
                        }
                        break;
                    case FetchState.Graphics:
                        address = (extraColorMode ? 0x39FF : 0x3FFF);
                        if (!idleState)
                        {
                            if (bitmapMode)
                                address &= (rowCounter | (videoCounter << 3) | (characterBitmap & 0x2000));
                            else
                                address &= (rowCounter | (characterData << 3) | characterBitmap);
                            videoMatrixLineIndex++;
                            videoMatrixLineIndex &= 0x3F;
                        }
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
                        refreshCounter--;
                        refreshCounter &= 0xFF;
                        break;
                    case FetchState.Sprite:
                        address = data | sprites[spriteIndex].Counter;
                        data = ReadRam(address);
                        break;
                }

                // render 4 pixels
                for (int i = 0; i < 4; i++)
                {
                    // pixelbuffer -> videobuffer
                    if (!hBlank && !vBlank)
                    {
                        videoBuffer[videoBufferIndex] = palette[pixelBuffer[pixelBufferIndex]];
                        videoBufferIndex++;
                    }

                    // graphics generator
                    if ((rasterX & 0x7) == xScroll)
                    {
                        graphicsGeneratorCharacter = characterData;
                        graphicsGeneratorColor = colorData;
                        graphicsGeneratorData = graphicsData;
                    }

                    // shift graphics data
                    if (!multiColorMode || (!bitmapMode && ((colorData & 0x4) == 0)))
                    {
                        graphicsGeneratorPixelData = graphicsData & 0x01;
                        graphicsData >>= 1;
                    }
                    else if ((rasterX & 0x7) == xScroll)
                    {
                        graphicsGeneratorPixelData = graphicsData & 0x03;
                        graphicsData >>= 2;
                    }

                    // generate pixel
                    if (!verticalBorder)
                    {
                        if (extraColorMode)
                        {
                            if (bitmapMode)
                            {
                                // ECM=1, BMM=1, MCM=1
                                // ECM=1, BMM=1, MCM=0
                                graphicsGeneratorPixel = 0;
                            }
                            else
                            {
                                if (multiColorMode)
                                {
                                    // ECM=1, BMM=0, MCM=1
                                    graphicsGeneratorPixel = 0;
                                }
                                else
                                {
                                    // ECM=1, BMM=0, MCM=0
                                    if (graphicsGeneratorPixelData == 0)
                                        graphicsGeneratorPixel = backgroundColor[characterData >> 6];
                                    else
                                        graphicsGeneratorPixel = colorData;
                                }
                            }
                        }
                        else
                        {
                            if (bitmapMode)
                            {
                                if (multiColorMode)
                                {
                                    // ECM=0, BMM=1, MCM=1
                                    if (graphicsGeneratorPixelData == 0x0)
                                        graphicsGeneratorPixel = backgroundColor[0];
                                    else if (graphicsGeneratorPixelData == 0x1)
                                        graphicsGeneratorPixel = characterData >> 4;
                                    else if (graphicsGeneratorPixelData == 0x2)
                                        graphicsGeneratorPixel = (characterData & 0xF);
                                    else
                                        graphicsGeneratorPixel = colorData;
                                }
                                else
                                {
                                    // ECM=0, BMM=1, MCM=0
                                    if (graphicsGeneratorPixelData == 0x0)
                                        graphicsGeneratorPixel = (characterData & 0xF);
                                    else
                                        graphicsGeneratorPixel = characterData >> 4;
                                }
                            }
                            else
                            {
                                if (multiColorMode)
                                {
                                    // ECM=0, BMM=0, MCM=1
                                    if ((colorData & 0x4) == 0)
                                    {
                                        if (graphicsGeneratorPixelData == 0x0)
                                            graphicsGeneratorPixel = backgroundColor[0];
                                        else
                                            graphicsGeneratorPixel = (colorData & 0x7);
                                    }
                                    else
                                    {
                                        if (graphicsGeneratorPixelData == 0x0)
                                            graphicsGeneratorPixel = backgroundColor[0];
                                        else if (graphicsGeneratorPixelData == 0x1)
                                            graphicsGeneratorPixel = backgroundColor[1];
                                        else if (graphicsGeneratorPixelData == 0x2)
                                            graphicsGeneratorPixel = backgroundColor[2];
                                        else
                                            graphicsGeneratorPixel = (colorData & 0x7);
                                    }
                                }
                                else
                                {
                                    // ECM=0, BMM=0, MCM=0
                                    if (graphicsGeneratorPixelData == 0x0)
                                        graphicsGeneratorPixel = backgroundColor[0];
                                    else
                                        graphicsGeneratorPixel = colorData;
                                }
                            }
                        }
                    }
                    else
                    {
                        // vertical border enabled, disable output
                        graphicsGeneratorPixel = backgroundColor[0];
                        graphicsGeneratorPixelData = 0x0;
                    }

                    // pixel generator -> pixelbuffer
                    pixelBuffer[pixelBufferIndex] = graphicsGeneratorPixel;

                    // border unit comparisons
                    if (rasterX == verticalBorderStart)
                        mainBorder = true;
                    else if (rasterX == verticalBorderEnd)
                    {
                        if (rasterY == mainBorderStart)
                            verticalBorder = true;
                        if (rasterY == mainBorderEnd && displayEnable)
                            verticalBorder = false;
                        if (!verticalBorder)
                            mainBorder = false;
                    }

                    // border unit -> pixelbuffer
                    if (mainBorder || verticalBorder)
                        pixelBuffer[borderPixelBufferIndex] = borderColor;

                    // advance pixelbuffer
                    pixelBufferIndex++;
                    if (pixelBufferIndex == pixelBufferLength)
                        pixelBufferIndex = 0;
                    borderPixelBufferIndex++;
                    if (borderPixelBufferIndex == pixelBufferLength)
                        borderPixelBufferIndex = 0;

                    // horizontal raster delay found in 6567R8
                    if (rasterDelay > 0)
                        rasterDelay--;
                    else
                        rasterX++;
                }

                if (!phi0)
                    phi1Data = data;

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
            pixelDataBuffer = new int[pixelBufferLength];
            pixelBufferIndex = 0;
            borderPixelBufferIndex = 8;

            // internal screen row buffer
            colorMatrix = new int[40];
            characterMatrix = new int[40];
            rowCounter = 0;
            videoCounter = 0;
            videoCounterBase = 0;
            videoMatrixLineIndex = 0;

            // border unit
            mainBorder = true;
            verticalBorder = true;
        }
    }
}
