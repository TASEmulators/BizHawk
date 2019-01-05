
namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// 128K/+2 ULA
    /// </summary>
    class ScreenPentagon128 : ULA
    {
        #region Construction

        public ScreenPentagon128(SpectrumBase machine)
			: base(machine)
        {
			// interrupt
			InterruptStartTime = 0;// 3;
            InterruptLength = 32;
            // offsets
            RenderTableOffset = 58;
            ContentionOffset = 6;
            FloatingBusOffset = 1;
            // timing
            ClockSpeed = 3546900;
            FrameCycleLength = 71680;
            ScanlineTime = 224;
            BorderLeftTime = 24;
            BorderRightTime = 24;
            FirstPaperLine = 80;
            FirstPaperTState = 68;
            // screen layout
            Border4T = false;
            Border4TStage = 1;            
            ScreenWidth = 256;
            ScreenHeight = 192;
            BorderTopHeight = 48; // 55; // 48;
            BorderBottomHeight = 48; // 56;
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
