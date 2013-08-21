using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    sealed public partial class Vic
    {
        const int AEC_DELAY = 7;
        const int GRAPHICS_GENERATOR_DELAY = 12;
        const int BORDER_GENERATOR_DELAY = 8;
        const int BORDER_GENERATOR_DELAY_BIT = 1 << BORDER_GENERATOR_DELAY;

        int address;
        bool aec;
        int aecCounter;
        bool ba;
        bool badLineCondition;
        bool badLineEnable;
        int borderDelay;
        int characterData;
        int[] characterMatrix;
        int colorData;
        int[] colorMatrix;
        int data;
        int graphicsData;
        int graphicsGeneratorCharacter;
        int graphicsGeneratorColor;
        int graphicsGeneratorData;
        bool graphicsGeneratorMulticolor;
        int graphicsGeneratorPixel;
        int graphicsGeneratorPixelData;
        bool graphicsGeneratorShiftToggle;
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
        Sprite spriteBuffer;
        int spriteGeneratorBackgroundData;
        int spriteGeneratorPixel;
        int spriteGeneratorPixelData;
        bool spriteGeneratorPixelEnabled;
        bool spriteGeneratorPriority;
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
            pixelBufferLength = GRAPHICS_GENERATOR_DELAY;
            Reset();
        }

        public void Clock()
        {
            // these should be cached somewhere
            mainBorderStart = columnSelect ? 0x18 : 0x1F;
            mainBorderEnd = columnSelect ? 0x158 : 0x14F;
            verticalBorderStart = rowSelect ? 0x33 : 0x37;
            verticalBorderEnd = rowSelect ? 0xFB : 0xF7;

            // process badline enable & condition
            if (rasterY >= 0x30 && rasterY < 0xF8)
            {
                if (rasterY == 0x30 && displayEnable)
                    badLineEnable = true;
                if (badLineEnable && ((rasterY & 0x7) == yScroll))
                    badLineCondition = true;
            }

            // process sprites on phi1
            foreach (Sprite sprite in sprites)
            {
                // process expansion flipflop
                if (!sprite.ExpandY)
                    sprite.ExpandYToggle = true;
                else if (rasterX == spriteDMACheckStart)
                    sprite.ExpandYToggle = !sprite.ExpandYToggle;
            }

            // process sprite dma enable
            if (rasterX == spriteDMACheckStart || rasterX == spriteDMACheckEnd)
            {
                foreach (Sprite sprite in sprites)
                {
                    if (sprite.Enabled && !sprite.DMA & sprite.Y == (rasterY & 0xFF))
                    {
                        sprite.DMA = true;
                        sprite.CounterBase = 0;
                        if (sprite.ExpandY)
                            sprite.ExpandYToggle = false;
                    }

                    //TODO: VERIFY THIS IS THE CORRECT TIMING
                    // (the VIC doc I used doesn't specify exactly when this happens)
                    sprite.DataShiftEnable = false;
                }
            }

            // process sprite display
            if (rasterX == spriteCounterCheckStart)
            {
                foreach (Sprite sprite in sprites)
                {
                    sprite.Counter = sprite.CounterBase;
                    if (sprite.DMA && sprite.Y == (rasterY & 0xFF))
                        sprite.Display = true;
                }
            }

            // process sprite counter base
            if (rasterX == spriteDMADisableStart)
            {
                foreach (Sprite sprite in sprites)
                {
                    if (sprite.ExpandYToggle)
                        sprite.CounterBase += 2;
                }
            }

            // process sprite dma disable
            if (rasterX == spriteDMADisableEnd)
            {
                foreach (Sprite sprite in sprites)
                {
                    if (sprite.ExpandYToggle)
                        sprite.CounterBase += 1;
                    if (sprite.CounterBase == 63)
                    {
                        sprite.DMA = false;
                        sprite.Display = false;
                    }
                }
            }


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
                        badLineEnable = false;
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
                            if (sprite.DMA)
                            {
                                fetchState = FetchState.Sprite;
                                ba = false;
                            }
                            break;
                        }
                        else if (rasterX == sprite.FetchStart)
                        {
                            sprite.Fetch = true;
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
                    aecCounter = AEC_DELAY;
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
                        sprites[spriteIndex].Pointer = address;
                        break;
                    case FetchState.Refresh:
                        address = refreshCounter | 0x3F00;
                        data = ReadRam(address);
                        refreshCounter--;
                        refreshCounter &= 0xFF;
                        break;
                    case FetchState.Sprite:
                        spriteBuffer = sprites[spriteIndex];
                        address = (spriteBuffer.Pointer << 6) | spriteBuffer.Counter;
                        data = ReadRam(address);
                        spriteBuffer.Counter++;
                        spriteBuffer.Counter &= 0x3F;
                        spriteBuffer.Data <<= 8;
                        spriteBuffer.Data |= data;
                        break;
                }

                // render 4 pixels (there are 8 per cycle)
                for (int i = 0; i < 4; i++)
                {
                    // initialize background pixel data generator
                    if ((rasterX & 0x7) == xScroll)
                    {
                        graphicsGeneratorCharacter = characterData;
                        graphicsGeneratorColor = colorData;
                        graphicsGeneratorData = graphicsData;
                        graphicsGeneratorMulticolor = !(!multiColorMode || (!bitmapMode && ((colorData & 0x4) == 0)));
                        graphicsGeneratorShiftToggle = !graphicsGeneratorMulticolor;
                    }

                    // shift graphics data
                    if (graphicsGeneratorShiftToggle)
                        graphicsGeneratorPixelData >>= graphicsGeneratorMulticolor ? 2 : 1;
                    graphicsGeneratorShiftToggle = !graphicsGeneratorShiftToggle || !graphicsGeneratorMulticolor;

                    // generate data and color for the pixelbuffer
                    if (!verticalBorder)
                    {
                        // graphics generator
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

                    // sprite generator
                    spriteGeneratorBackgroundData = pixelDataBuffer[pixelBufferIndex];
                    spriteIndex = 0;
                    spriteGeneratorPixelEnabled = false;
                    foreach (Sprite sprite in sprites)
                    {
                        if (sprite.Display)
                        {
                            if (sprite.X == rasterX)
                            {
                                // enable sprite shift register on X compare
                                sprite.DataShiftEnable = true;
                                sprite.ExpandXToggle = !sprite.ExpandX;
                                sprite.MultiColorToggle = !sprite.Multicolor;
                            }

                            if (sprite.DataShiftEnable)
                            {
                                // bit select based on multicolor
                                if (sprite.Multicolor)
                                    sprite.OutputData = sprite.Data & 0x300000;
                                else
                                    sprite.OutputData = sprite.Data & 0x200000;

                                // shift bits in the shift register
                                if (sprite.MultiColorToggle && sprite.ExpandXToggle)
                                    sprite.Data <<= sprite.Multicolor ? 2 : 1;

                                // flipflops used to determine when to shift bits
                                sprite.MultiColorToggle = !sprite.MultiColorToggle || !sprite.Multicolor;
                                if (sprite.MultiColorToggle)
                                    sprite.ExpandXToggle = !sprite.ExpandXToggle || !sprite.ExpandX;

                                // determine sprite collision and color
                                if (sprite.OutputData != 0x000000)
                                {
                                    if (!spriteGeneratorPixelEnabled)
                                    {
                                        spriteGeneratorPixelEnabled = true;
                                        spriteGeneratorPixelData = spriteIndex;
                                        spriteGeneratorPriority = sprite.Priority;

                                        // determine sprite pixel output for topmost sprite only
                                        if (sprite.OutputData == 0x100000)
                                            sprite.OutputPixel = spriteMultiColor[0];
                                        else if (sprite.OutputData == 0x200000)
                                            sprite.OutputPixel = sprite.Color;
                                        else if (sprite.OutputData == 0x300000)
                                            sprite.OutputPixel = spriteMultiColor[1];
                                    }
                                    else
                                    {
                                        sprites[spriteGeneratorPixelData].SpriteCollision = true;
                                        sprite.SpriteCollision = true;
                                    }

                                    // determine sprite-background collision
                                    if ((spriteGeneratorBackgroundData & 0x2) != 0)
                                        sprite.DataCollision = true;
                                }

                            }
                        }
                        spriteIndex++;
                    }

                    // combine the pixels
                    if (spriteGeneratorPixelEnabled && (!spriteGeneratorPriority || ((spriteGeneratorBackgroundData & 0x2) == 0)))
                        pixel = spriteGeneratorPixel;
                    else
                        pixel = pixelBuffer[pixelBufferIndex];

                    // pixel generator data -> pixeldatabuffer
                    pixelDataBuffer[pixelBufferIndex] = graphicsGeneratorPixelData;
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

                    // border unit (delay of 8 pixels, we use a shift register)
                    borderDelay <<= 1;
                    if (mainBorder || verticalBorder)
                        borderDelay |= 1;
                    if ((borderDelay & BORDER_GENERATOR_DELAY_BIT) != 0)
                        pixel = borderColor;

                    // rendered pixel -> videobuffer
                    if (!hBlank && !vBlank)
                    {
                        videoBuffer[videoBufferIndex] = palette[pixel];
                        videoBufferIndex++;
                    }

                    // advance pixelbuffer
                    pixelBufferIndex++;
                    if (pixelBufferIndex == pixelBufferLength)
                        pixelBufferIndex = 0;

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
