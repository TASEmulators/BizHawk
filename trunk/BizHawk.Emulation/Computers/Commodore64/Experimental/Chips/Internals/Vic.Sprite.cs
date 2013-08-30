using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    sealed public partial class Vic
    {
        sealed class Sprite
        {
            public int Color;
            public bool DataCollision;
            public bool Enabled;
            public bool ExpandX;
            public bool ExpandY;
            public bool Multicolor;
            public bool Priority;
            public bool SpriteCollision;
            public int X;
            public int Y;

            public Sprite()
            {
            }

            public void Clock()
            {
            }

            public void LoadP(int value)
            {
            }

            public void LoadS(int value)
            {
            }
        }

    }
}
