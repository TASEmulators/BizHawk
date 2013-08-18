using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
    public abstract partial class Vic
    {
        protected const int baResetCounter = 6;
        protected const int pipelineUpdateVc = 1;
        protected const int pipelineChkSprChunch = 2;
        protected const int pipelineUpdateMcBase = 4;
        protected const int pipelineChkBrdL1 = 8;
        protected const int pipelineChkBrdL0 = 16;
        protected const int pipelineChkSprDma = 32;
        protected const int pipelineChkBrdR0 = 64;
        protected const int pipelineChkSprExp = 128;
        protected const int pipelineChkBrdR1 = 256;
        protected const int pipelineChkSprDisp = 512;
        protected const int pipelineUpdateRc = 1024;
        protected const int rasterIrqLine0Cycle = 1;
        protected const int rasterIrqLineXCycle = 0;

        private int parseaddr;
        private int parsecycleBAsprite0;
        private int parsecycleBAsprite1;
        private int parsecycleBAsprite2;
        private int parsecycleFetchSpriteIndex;
        private int parsefetch;
        private int parsefetchType;
        private int parseba;
        private int parseact;

        private void ParseCycle()
        {
            {
                parseaddr = 0x3FFF;
                parsefetch = pipeline[1][cycleIndex];
                parseba = pipeline[2][cycleIndex];
                parseact = pipeline[3][cycleIndex];

                // apply X location
                rasterX = pipeline[0][cycleIndex];

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
                    // fetch C
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
                    dataC = 0;
                }
                else if (parsefetchType == 0x500)
                {
                    // fetch none
                }
                else
                {
                    parsecycleFetchSpriteIndex = (parsefetch & 0x7);
                    switch (parsefetch & 0xF0)
                    {
                        case 0x00:
                            // fetch P
                            parseaddr = (0x3F8 | (pointerVM << 10) | parsecycleFetchSpriteIndex);
                            sprites[parsecycleFetchSpriteIndex].pointer = ReadMemory(parseaddr);
                            sprites[parsecycleFetchSpriteIndex].shiftEnable = false;
                            break;
                        case 0x10:
                        case 0x20:
                        case 0x30:
                            // fetch S
                            if (sprites[parsecycleFetchSpriteIndex].dma)
                            {
                                Sprite spr = sprites[parsecycleFetchSpriteIndex];
                                parseaddr = (spr.mc | (spr.pointer << 6));
                                spr.sr <<= 8;
                                spr.sr |= ReadMemory(parseaddr);
                                spr.mc++;
                            }
                            break;
                    }
                }

                // perform BA flag manipulation
                switch (parseba)
                {
                    case 0x0000:
                        pinBA = true;
                        break;
                    case 0x1000:
                        pinBA = !badline;
                        break;
                    default:
                        parsecycleBAsprite0 = (parseba & 0x000F);
                        parsecycleBAsprite1 = (parseba & 0x00F0) >> 4;
                        parsecycleBAsprite2 = (parseba & 0x0F00) >> 8;
                        if ((parsecycleBAsprite0 < 8 && sprites[parsecycleBAsprite0].dma) ||
                            (parsecycleBAsprite1 < 8 && sprites[parsecycleBAsprite1].dma) ||
                            (parsecycleBAsprite2 < 8 && sprites[parsecycleBAsprite2].dma))
                            pinBA = false;
                        else
                            pinBA = true;
                        break;
                }

                // perform actions
                borderCheckLEnable = true;
                borderCheckREnable = true;

                if ((parseact & pipelineChkSprChunch) != 0)
                {
                    //for (int i = 0; i < 8; i++)
                    foreach (Sprite spr in sprites)
                    {
                        //Sprite spr = sprites[i];
                        if (spr.yCrunch)
                            spr.mcbase += 2;
                        spr.shiftEnable = false;
                        spr.xCrunch = !spr.xExpand;
                        spr.multicolorCrunch = !spr.multicolor;
                    }
                }
                if ((parseact & pipelineChkSprDisp) != 0)
                {
                    //for (int i = 0; i < 8; i++)
                    foreach (Sprite spr in sprites)
                    {
                        //Sprite spr = sprites[i];
                        spr.mc = spr.mcbase;
                        if (spr.dma && spr.y == (rasterLine & 0xFF))
                        {
                            spr.display = true;
                        }
                    }
                }
                if ((parseact & pipelineChkSprDma) != 0)
                {
                    //for (int i = 0; i < 8; i++)
                    foreach (Sprite spr in sprites)
                    {
                        //Sprite spr = sprites[i];
                        if (spr.enable && spr.y == (rasterLine & 0xFF) && !spr.dma)
                        {
                            spr.dma = true;
                            spr.mcbase = 0;
                            spr.yCrunch = !spr.yExpand;
                        }
                    }
                }
                if ((parseact & pipelineChkSprExp) != 0)
                {
                    if (sprites[0].yExpand) sprites[0].yCrunch ^= true;
                    if (sprites[1].yExpand) sprites[1].yCrunch ^= true;
                    if (sprites[2].yExpand) sprites[2].yCrunch ^= true;
                    if (sprites[3].yExpand) sprites[3].yCrunch ^= true;
                    if (sprites[4].yExpand) sprites[4].yCrunch ^= true;
                    if (sprites[5].yExpand) sprites[5].yCrunch ^= true;
                    if (sprites[6].yExpand) sprites[6].yCrunch ^= true;
                    if (sprites[7].yExpand) sprites[7].yCrunch ^= true;
                }
                if ((parseact & pipelineUpdateMcBase) != 0)
                {
                    //for (int i = 0; i < 8; i++)
                    foreach (Sprite spr in sprites)
                    {
                        //Sprite spr = sprites[i];
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
                if ((parseact & pipelineUpdateRc) != 0)
                {
                    if (rc == 7)
                    {
                        idle = true;
                        vcbase = vc;
                    }
                    if (!idle)
                        rc = (rc + 1) & 0x7;
                }
                if ((parseact & pipelineUpdateVc) != 0)
                {
                    vc = vcbase;
                    vmli = 0;
                    if (badline)
                        rc = 0;
                }

                cycleIndex++;
            }
        }
    }
}
