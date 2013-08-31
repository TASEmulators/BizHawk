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
        bool ba;
        int data;
        int phi1Data;
        int rasterX;

        public Vic(VicSettings settings)
        {
        }

        public void Clock()
        {
            Render();
        }

        public void Reset()
        {
        }
    }
}
