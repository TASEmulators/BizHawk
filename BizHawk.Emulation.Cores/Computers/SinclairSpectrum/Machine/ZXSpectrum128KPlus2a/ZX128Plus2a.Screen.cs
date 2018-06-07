using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    class Screen128Plus2a : ULA
    {
        #region Construction

        public Screen128Plus2a(SpectrumBase machine)
			: base(machine)
        {
            // timing
            ClockSpeed = 3546900;
            FrameCycleLength = 70908;
            InterruptStartTime = 33;
            InterruptLength = 32;
            ScanlineTime = 228;

            MemoryContentionOffset = 7;
            PortContentionOffset = 7;

            BorderLeftTime = 24;
            BorderRightTime = 24;

            FirstPaperLine = 63;
            FirstPaperTState = 64;

            Border4T = true;
            Border4TStage = 2;

            // screen layout
            ScreenWidth = 256;
            ScreenHeight = 192;
            BorderTopHeight = 48;
            BorderBottomHeight = 56;
            BorderLeftWidth = 48;
            BorderRightWidth = 48;
            ScanLineWidth = BorderLeftWidth + ScreenWidth + BorderRightWidth;

            RenderingTable = new RenderTable(this,
                MachineType.ZXSpectrum128Plus2a);

            SetupScreenSize();
        }

        #endregion
    }
}
