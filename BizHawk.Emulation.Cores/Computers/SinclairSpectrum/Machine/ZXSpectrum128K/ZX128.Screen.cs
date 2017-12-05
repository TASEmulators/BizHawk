using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    public partial class ZX128 : SpectrumBase
    {
        public override void InitScreenConfig()
        {

            ScreenLines = BorderTopLines + DisplayLines + BorderBottomLines;
            FirstDisplayLine = VerticalSyncLines + NonVisibleBorderTopLines + BorderTopLines;
            LastDisplayLine = FirstDisplayLine + DisplayLines - 1;
            ScreenWidth = BorderLeftPixels + DisplayWidth + BorderRightPixels;
            FirstPixelCycleInLine = HorizontalBlankingTime + BorderLeftTime;
            ScreenLineTime = FirstPixelCycleInLine + DisplayLineTime + BorderRightTime + NonVisibleBorderRightTime;
            UlaFrameCycleCount = (FirstDisplayLine + DisplayLines + BorderBottomLines + NonVisibleBorderTopLines) * ScreenLineTime;
            FirstScreenPixelCycle = (VerticalSyncLines + NonVisibleBorderTopLines) * ScreenLineTime + HorizontalBlankingTime;
        }
    }
}
