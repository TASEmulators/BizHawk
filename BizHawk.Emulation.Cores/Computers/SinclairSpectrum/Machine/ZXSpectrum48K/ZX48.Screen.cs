using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    class Screen48 : ULA
    {
        #region Construction

		public Screen48(SpectrumBase machine)
			: base(machine)
        {
			// timing
			ClockSpeed = 3500000;
            FrameCycleLength = 69888;
            InterruptStartTime = 33;
            InterruptLength = 32;
            ScanlineTime = 224;

            MemoryContentionOffset = 6;
            PortContentionOffset = 6;
            RenderTableOffset = -9; // 2;
            FloatingBusOffset = 1;

            BorderLeftTime = 24;
            BorderRightTime = 24;

            FirstPaperLine = 64;
            FirstPaperTState = 64;

            Border4T = true;
            Border4TStage = 0;

            // screen layout
            ScreenWidth = 256;
            ScreenHeight = 192;
            BorderTopHeight = 55;// 48;
            BorderBottomHeight = 56;
            BorderLeftWidth = 48;
            BorderRightWidth = 48;
            ScanLineWidth = BorderLeftWidth + ScreenWidth + BorderRightWidth;

            RenderingTable = new RenderTable(this,
                MachineType.ZXSpectrum48);

            SetupScreenSize();
        }

        #endregion
    }
}
