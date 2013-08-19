using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
    public abstract partial class Vic
    {
        protected class Sprite
        {
            public bool collideData;
            public bool collideSprite;
            public int color;
            public bool display;
            public bool dma;
            public bool enable;
            public int mc;
            public int mcbase;
            public bool multicolor;
            public bool multicolorCrunch;
            public int pointer;
            public bool priority;
            public bool shiftEnable;
            public int sr;
            public int x;
            public bool xCrunch;
            public bool xExpand;
            public int y;
            public bool yCrunch;
            public bool yExpand;

            public void HardReset()
            {
                collideData = false;
                collideSprite = false;
                color = 0;
                display = false;
                dma = false;
                enable = false;
                mc = 0;
                mcbase = 0;
                multicolor = false;
                multicolorCrunch = false;
                pointer = 0;
                priority = false;
                shiftEnable = false;
                sr = 0;
                x = 0;
                xCrunch = false;
                xExpand = false;
                y = 0;
                yCrunch = false;
                yExpand = false;
            }

            public void SyncState(Serializer ser)
            {
                ser.Sync("collideData", ref collideData);
                ser.Sync("collideSprite", ref collideSprite);
                ser.Sync("color", ref color);
                ser.Sync("display", ref display);
                ser.Sync("dma", ref dma);
                ser.Sync("enable", ref enable);
                ser.Sync("mc", ref mc);
                ser.Sync("mcbase", ref mcbase);
                ser.Sync("multicolor", ref multicolor);
                ser.Sync("multicolorCrunch", ref multicolorCrunch);
                ser.Sync("pointer", ref pointer);
                ser.Sync("priority", ref priority);
                ser.Sync("shiftEnable", ref shiftEnable);
                ser.Sync("sr", ref sr);
                ser.Sync("x", ref x);
                ser.Sync("xCrunch", ref xCrunch);
                ser.Sync("xExpand", ref xExpand);
                ser.Sync("y", ref y);
                ser.Sync("yCrunch", ref yCrunch);
                ser.Sync("yExpand", ref yExpand);
            }
        }
    }
}
