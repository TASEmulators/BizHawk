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
            public bool BA;
            public int BAEnd; //precalculated
            public int BAStart; //precalculated
            public int Counter; //MC
            public int CounterBase; //MCBASE
            public int Data; //24-bit shift register
            public bool Fetch;
            public int FetchStart; //precalculated

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
                BA = true;
            }
        }

    }
}
