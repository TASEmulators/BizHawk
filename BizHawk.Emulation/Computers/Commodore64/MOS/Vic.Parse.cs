using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
    sealed public partial class Vic
    {
        const int baResetCounter = 7;
        const int pipelineUpdateVc = 1;
        const int pipelineChkSprChunch = 2;
        const int pipelineUpdateMcBase = 4;
        const int pipelineChkBrdL1 = 8;
        const int pipelineChkBrdL0 = 16;
        const int pipelineChkSprDma = 32;
        const int pipelineChkBrdR0 = 64;
        const int pipelineChkSprExp = 128;
        const int pipelineChkBrdR1 = 256;
        const int pipelineChkSprDisp = 512;
        const int pipelineUpdateRc = 1024;
        const int pipelineHBlankL = 0x10000000;
        const int pipelineHBlankR = 0x20000000;
        const int pipelineHoldX = 0x40000000;
        const int rasterIrqLine0Cycle = 1;
        const int rasterIrqLineXCycle = 0;

        int parseaddr;
        int parsecycleBAsprite0;
        int parsecycleBAsprite1;
        int parsecycleBAsprite2;
        int parsecycleFetchSpriteIndex;
        int parsefetch;
        int parsefetchType;
        int parseba;
        int parseact;

        private void ParseCycle()
        {
            {
                parseaddr = 0x3FFF;
                parsefetch = pipeline[1][cycleIndex];
                parseba = pipeline[2][cycleIndex];
                parseact = pipeline[3][cycleIndex];

                // apply X location
                rasterX = pipeline[0][cycleIndex];
                rasterXHold = ((parseact & pipelineHoldX) != 0);

                // perform fetch
                parsefetchType = parsefetch & 0xFF00;
                if (parsefetchType == 0x100)
                {
                    // fetch R
                    refreshCounter = (refreshCounter - 1) & 0xFF;
                    parseaddr = (0x3F00 | refreshCounter);
                    ReadMemory(parseaddr);
                }
                else if (parsefetchType == 0x200)
                {
                    delayC = xScroll;
                    if (!idle)
                    {
                        if (badline)
                        {
                            parseaddr = ((pointerVM << 10) | vc);
                            dataC = ReadMemory(parseaddr);
                            dataC |= ((int)ReadColorRam(parseaddr) & 0xF) << 8;
                            bufferC[vmli] = dataC;
                        }
                        else
                        {
                            dataC = bufferC[vmli];
                        }
                    }
                    else
                    {
                        dataC = 0;
                        bufferC[vmli] = dataC;
                    }
                    srC <<= 12;
                    srC |= dataC;
                }
                else if (parsefetchType == 0x300)
                {
                    // fetch G
                    if (idle)
                        parseaddr = 0x3FFF;
                    else
                    {
                        if (bitmapMode)
                            parseaddr = (rc | (vc << 3) | ((pointerCB & 0x4) << 11));
                        else
                            parseaddr = (rc | ((dataC & 0xFF) << 3) | (pointerCB << 11));
                    }
                    if (extraColorMode)
                        parseaddr &= 0x39FF;
                    dataG = ReadMemory(parseaddr);
                    sr |= dataG << (7 - xScroll);
                    srSync |= 0xAA << (7 - xScroll);
                    if (!idle)
                    {
                        bufferG[vmli] = dataG;
                        vmli = (vmli + 1) & 0x3F;
                        vc = (vc + 1) & 0x3FF;
                    }
                }
                else if (parsefetchType == 0x400)
                {
                    // fetch I
                    parseaddr = (extraColorMode ? 0x39FF : 0x3FFF);
                    dataG = ReadMemory(parseaddr);
                    //dataC = 0;
                }
                else if (parsefetchType == 0x500)
                {
                    // fetch none
                }
                else
                {
                    parsecycleFetchSpriteIndex = (parsefetch & 0x7);
                    if ((parsefetch & 0xF0) == 0)
                    {
                        // fetch P
                        parseaddr = (0x3F8 | (pointerVM << 10) | parsecycleFetchSpriteIndex);
                        sprites[parsecycleFetchSpriteIndex].pointer = ReadMemory(parseaddr);
                        sprites[parsecycleFetchSpriteIndex].shiftEnable = false;
                    }
                    else
                    {
                        // fetch S
                        if (sprites[parsecycleFetchSpriteIndex].dma)
                        {
                            SpriteGenerator spr = sprites[parsecycleFetchSpriteIndex];
                            parseaddr = (spr.mc | (spr.pointer << 6));
                            spr.sr <<= 8;
                            spr.sr |= ReadMemory(parseaddr);
                            spr.mc++;
                        }
                    }
                }

                // perform BA flag manipulation
                if (parseba == 0x0000)
                {
                    pinBA = true;
                }
                else if (parseba == 0x1000)
                {
                    pinBA = !badline;
                }
                else
                {
                    parsecycleBAsprite0 = (parseba & 0x000F);
                    parsecycleBAsprite1 = (parseba & 0x00F0) >> 4;
                    parsecycleBAsprite2 = (parseba & 0x0F00) >> 8;
                    if ((parsecycleBAsprite0 < 8 && sprites[parsecycleBAsprite0].dma) ||
                        (parsecycleBAsprite1 < 8 && sprites[parsecycleBAsprite1].dma) ||
                        (parsecycleBAsprite2 < 8 && sprites[parsecycleBAsprite2].dma))
                        pinBA = false;
                    else
                        pinBA = true;
                }

                // perform actions
                borderCheckLEnable = ((parseact & (pipelineChkBrdL0 | pipelineChkBrdL1)) != 0);
                borderCheckREnable = ((parseact & (pipelineChkBrdR0 | pipelineChkBrdR1)) != 0);
                hblankCheckEnableL = ((parseact & pipelineHBlankL) != 0);
                hblankCheckEnableR = ((parseact & pipelineHBlankR) != 0);

                if (parseact != 0)
                {
                    if ((parseact & pipelineChkSprChunch) != 0)
                    {
                        foreach (SpriteGenerator spr in sprites)
                        {
                            if (spr.yCrunch)
                                spr.mcbase += 2;
                            spr.shiftEnable = false;
                            spr.xCrunch = !spr.xExpand;
                            spr.multicolorCrunch = !spr.multicolor;
                        }
                    }

                    else if ((parseact & pipelineChkSprDisp) != 0)
                    {
                        foreach (SpriteGenerator spr in sprites)
                        {
                            spr.mc = spr.mcbase;
                            if (spr.dma && spr.y == (rasterLine & 0xFF))
                            {
                                spr.display = true;
                            }
                        }
                    }

                    else if ((parseact & pipelineChkSprDma) != 0)
                    {
                        foreach (SpriteGenerator spr in sprites)
                        {
                            if (spr.enable && spr.y == (rasterLine & 0xFF) && !spr.dma)
                            {
                                spr.dma = true;
                                spr.mcbase = 0;
                                spr.yCrunch = !spr.yExpand;
                            }
                        }
                    }

                    else if ((parseact & pipelineChkSprExp) != 0)
                    {
                        foreach (SpriteGenerator spr in sprites)
                        {
                            if (spr.yExpand)
                                spr.yCrunch ^= true;
                        }
                    }

                    else if ((parseact & pipelineUpdateMcBase) != 0)
                    {
                        foreach (SpriteGenerator spr in sprites)
                        {
                            if (spr.yCrunch)
                            {
                                spr.mcbase++;
                                if (spr.mcbase == 63)
                                {
                                    spr.dma = false;
                                    spr.display = false;
                                }
                            }
                        }
                    }

                    else if ((parseact & pipelineUpdateRc) != 0)
                    {
                        if (rc == 7)
                        {
                            idle = true;
                            vcbase = vc;
                        }
                        if (!idle)
                            rc = (rc + 1) & 0x7;
                    }

                    else if ((parseact & pipelineUpdateVc) != 0)
                    {
                        vc = vcbase;
                        vmli = 0;
                        if (badline)
                            rc = 0;
                    }
                }

                cycleIndex++;
            }
        }
    }
}
