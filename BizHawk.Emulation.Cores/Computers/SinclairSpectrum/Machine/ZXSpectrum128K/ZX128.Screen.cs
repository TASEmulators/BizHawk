using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    class Screen128 : ULA
    {
        #region Construction

        public Screen128(SpectrumBase machine)
			: base(machine)
        {
            // timing
            ClockSpeed = 3546900;
            FrameCycleLength = 70908;
            InterruptStartTime = 34;
            InterruptLength = 36;
            ScanlineTime = 228;

            MemoryContentionOffset = 6;
            PortContentionOffset = 6;
            RenderTableOffset = -4;
            FloatingBusOffset = 1;

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
                MachineType.ZXSpectrum128);

            SetupScreenSize();
        }

        #endregion
    }
}
