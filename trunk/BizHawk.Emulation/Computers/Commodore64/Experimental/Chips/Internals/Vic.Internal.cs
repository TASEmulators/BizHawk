using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    sealed public partial class Vic
    {
        const int AEC_DELAY = 7;
        const int BORDER_GENERATOR_DELAY = 8;
        const int BORDER_GENERATOR_DELAY_BIT = 1 << BORDER_GENERATOR_DELAY;
        const bool BORDER_ENABLE = false;

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
        int graphicsGeneratorOutputData;
        int graphicsGeneratorPipeData;
        int graphicsGeneratorPipePixel0;
        int graphicsGeneratorPipePixel2;
        int graphicsGeneratorPixel;
        bool graphicsGeneratorShiftToggle;
        bool idleState;
        bool mainBorder;
        int mainBorderEnd;
        int mainBorderStart;
        bool phi0;
        int phi1Data;
        int pixel;
        int rasterX;
        int refreshCounter;
        int rowCounter;
        Sprite spriteBuffer;
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
            timing = settings.timing;

            // reset registers
            Reset();

            // calculate visible width
            screenWidth = 0;
            rasterX = screenXStart;
            while (rasterX != screenXEnd)
            {
                screenWidth++;
                rasterX++;
                if (rasterX == rasterWidth)
                    rasterX = 0;
            }

            // calculate visible height
            screenHeight = 0;
            rasterY = screenYStart;
            while (rasterY != screenYEnd)
            {
                screenHeight++;
                rasterY++;
                if (rasterY == rasterCount)
                    rasterY = 0;
            }

            // reset raster counters
            rasterX = 0;
            rasterY = 0;

            // create video buffer
            videoBuffer = new int[screenWidth * screenHeight];
        }

        public void Clock()
        {
            // these should be cached somewhere
            mainBorderEnd = columnSelect ? 0x18 : 0x1F;
            mainBorderStart = columnSelect ? 0x158 : 0x14F;
            verticalBorderEnd = rowSelect ? 0x33 : 0x37;
            verticalBorderStart = rowSelect ? 0xFB : 0xF7;

            // process badline enable & condition
            if (!badLineCondition && rasterY >= 0x30 && rasterY < 0xF8)
            {
                if (rasterY == 0x30 && displayEnable)
                    badLineEnable = true;
                if (badLineEnable && ((rasterY & 0x7) == yScroll))
                {
                    badLineCondition = true;
                    idleState = false;
                }
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
                    if (badLineCondition)
                        rowCounter = 0;
                    if (rasterY == rasterYCompare)
                        rasterInterrupt = true;
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
                    badLineCondition = false;
                }

                // determine BA and fetch state
                ba = true;

                // None is used for when we don't assert control.
                fetchState = phi0 ? FetchState.None : FetchState.Idle;

                if (!characterBA && badLineCondition && badLineEnable)
                {
                    ba = false;
                }

                if (graphicsFetch)
                {
                    if (badLineCondition && badLineEnable)
                    {
                        fetchState = phi0 ? FetchState.Character : FetchState.Graphics;
                    }
                    else
                    {
                        fetchState = phi0 ? FetchState.CharacterInternal : FetchState.Graphics;
                    }
                }

                if (refreshFetch)
                {
                    // covers memory refresh fetches
                    fetchState = phi0 ? FetchState.None : FetchState.Refresh;
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
                            ba = !sprite.DMA;
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
                        else
                        {
                            colorData = 0;
                            characterData = 0;
                        }
                        break;
                    case FetchState.CharacterInternal:
                        colorData = colorMatrix[videoMatrixLineIndex];
                        characterData = characterMatrix[videoMatrixLineIndex];
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
                            videoCounter = ((videoCounter + 1) & 0x3FF);
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
                for (int j = 0; j < 4; j++)
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

                    // extract graphics data
                    graphicsGeneratorOutputData = (graphicsGeneratorData & (graphicsGeneratorMulticolor ? 0xC0 : 0x80));
                    graphicsGeneratorPipeData <<= 2;
                    graphicsGeneratorPipeData |= graphicsGeneratorOutputData;

                    // shift graphics data
                    if (graphicsGeneratorShiftToggle)
                        graphicsGeneratorData <<= graphicsGeneratorMulticolor ? 2 : 1;
                    graphicsGeneratorShiftToggle = !graphicsGeneratorShiftToggle || !graphicsGeneratorMulticolor;

                    // generate data and color for the pixelbuffer
                    if (!verticalBorder || !BORDER_ENABLE)
                    {
                        // graphics generator
                        if (extraColorMode && !bitmapMode && !multiColorMode)
                        {
                            // ECM=1, BMM=0, MCM=0
                            if (graphicsGeneratorOutputData == 0x00)
                                graphicsGeneratorPixel = backgroundColor[characterData >> 6];
                            else
                                graphicsGeneratorPixel = colorData;
                        }
                        else
                        {
                            if (bitmapMode)
                            {
                                if (multiColorMode)
                                {
                                    // ECM=0, BMM=1, MCM=1
                                    if (graphicsGeneratorOutputData == 0x00)
                                        graphicsGeneratorPixel = backgroundColor[0];
                                    else if (graphicsGeneratorOutputData == 0x40)
                                        graphicsGeneratorPixel = characterData >> 4;
                                    else if (graphicsGeneratorOutputData == 0x80)
                                        graphicsGeneratorPixel = (characterData & 0xF);
                                    else
                                        graphicsGeneratorPixel = colorData;
                                }
                                else
                                {
                                    // ECM=0, BMM=1, MCM=0
                                    if (graphicsGeneratorOutputData == 0x00)
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
                                        if (graphicsGeneratorOutputData == 0x00)
                                            graphicsGeneratorPixel = backgroundColor[0];
                                        else
                                            graphicsGeneratorPixel = (colorData & 0x7);
                                    }
                                    else
                                    {
                                        if (graphicsGeneratorOutputData == 0x00)
                                            graphicsGeneratorPixel = backgroundColor[0];
                                        else if (graphicsGeneratorOutputData == 0x40)
                                            graphicsGeneratorPixel = backgroundColor[1];
                                        else if (graphicsGeneratorOutputData == 0x80)
                                            graphicsGeneratorPixel = backgroundColor[2];
                                        else
                                            graphicsGeneratorPixel = (colorData & 0x7);
                                    }
                                }
                                else
                                {
                                    // ECM=0, BMM=0, MCM=0
                                    if (graphicsGeneratorOutputData == 0x00)
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
                    }

                    // shift color data
                    if (phi0)
                    {
                        graphicsGeneratorPipePixel2 <<= 4;
                        graphicsGeneratorPipePixel2 |= graphicsGeneratorPixel;
                        graphicsGeneratorPixel = (graphicsGeneratorPipePixel2 & 0x00F00000) >> 20;
                    }
                    else
                    {
                        graphicsGeneratorPipePixel0 <<= 4;
                        graphicsGeneratorPipePixel0 |= graphicsGeneratorPixel;
                        graphicsGeneratorPixel = (graphicsGeneratorPipePixel0 & 0x00F00000) >> 20;
                    }

                    // sprite generator
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
                                    if ((graphicsGeneratorData & 0x200000) != 0)
                                        sprite.DataCollision = true;
                                }

                            }
                        }
                        spriteIndex++;
                    }

                    // combine the pixels
                    if (spriteGeneratorPixelEnabled && (!spriteGeneratorPriority || ((graphicsGeneratorPipeData & 0x80000) == 0)))
                        pixel = spriteGeneratorPixel;
                    else
                        pixel = graphicsGeneratorPixel;

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
                    if (BORDER_ENABLE && (borderDelay & BORDER_GENERATOR_DELAY_BIT) != 0)
                        pixel = borderColor;

                    // rendered pixel -> videobuffer
                    if (!hBlank && !vBlank)
                    {
                        videoBuffer[videoBufferIndex] = palette[pixel];
                        videoBufferIndex++;
                    }

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

            // process irq
            irq = !(
                (rasterInterrupt && rasterInterruptEnable)
                );

            // at the end, clock other devices if applicable
            if (ClockPhi0 != null)
                ClockPhi0();
        }

        public void Reset()
        {
            // set up color arrays
            backgroundColor = new int[4];
            spriteMultiColor = new int[2];

            // set up sprites
            sprites = new Sprite[8];
            for (int i = 0; i < 8; i++)
                sprites[i] = new Sprite();
            for (int i = 0; i < 0x40; i++)
                Poke(i, 0);

            // set up pin state
            phi0 = false;
            irq = true;
            ba = true;
            aec = true;

            // we set these so no video is displayed before
            // the first frame starts
            vBlank = true;
            hBlank = true;

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

            // setup timing
            InitTiming();
        }
    }
}
