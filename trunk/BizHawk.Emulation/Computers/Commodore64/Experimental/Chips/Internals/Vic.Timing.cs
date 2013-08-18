using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    public partial class Vic
    {
        protected struct CycleTiming
        {
            public int CharacterCycle;
            public int RefreshCycle;
            public int SpriteCycle;
        }
        
        protected struct DisplayTiming
        {
            public int CyclesPerLine;
            public int HBlankStart;
            public int HBlankEnd;
            public int LeftXCoordinate;
            public int LinesPerFrame;
            public int VBlankStart;
            public int VBlankEnd;
            public int VisibleLines;
            public int VisiblePixels;
        }

        protected class VicTiming
        {
            public CycleTiming Cycle;
            public DisplayTiming Display;
        }

        enum FetchState
        {
            Idle,
            Graphics,
            Color,
            Refresh,
            Sprite,
            Pointer
        }

        int characterCycleCount;
        int refreshCycleCount;
        int spriteCycleCount;
    }
}
