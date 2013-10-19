using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
    sealed public partial class Vic
    {
        sealed class SpriteGenerator
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
                SaveState.SyncObject(ser, this);
            }
        }
    }
}
