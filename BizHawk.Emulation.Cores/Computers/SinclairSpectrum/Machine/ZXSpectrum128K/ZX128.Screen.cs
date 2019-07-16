
namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// 128K/+2 ULA
    /// </summary>
    class Screen128 : ULA
    {
        #region Construction

        public Screen128(SpectrumBase machine)
			: base(machine)
        {
            // interrupt
            InterruptStartTime = 3;
            InterruptLength = 36;
            // offsets
            RenderTableOffset = 58;
            ContentionOffset = 6;
            FloatingBusOffset = 1;
            // timing
            ClockSpeed = 3546900;
            FrameCycleLength = 70908;
            ScanlineTime = 228;
            BorderLeftTime = 24;
            BorderRightTime = 24;
            FirstPaperLine = 63;
            FirstPaperTState = 64;
            // screen layout
            Border4T = true;
            Border4TStage = 2;            
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
