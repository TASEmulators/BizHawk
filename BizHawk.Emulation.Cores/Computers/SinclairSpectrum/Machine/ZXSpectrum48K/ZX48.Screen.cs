
namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// 48K ULA
    /// </summary>
    class Screen48 : ULA
    {
        #region Construction

		public Screen48(SpectrumBase machine)
			: base(machine)
        {
            // interrupt
            InterruptStartTime = 3;
            InterruptLength = 32;
            // offsets
            RenderTableOffset = 56;
            ContentionOffset = 6;
            FloatingBusOffset = 1;   
            // timing
            ClockSpeed = 3500000;
            FrameCycleLength = 69888;
            ScanlineTime = 224;
            BorderLeftTime = 24;
            BorderRightTime = 24;
            FirstPaperLine = 64;
            FirstPaperTState = 64;
            // screen layout
            Border4T = true;
            Border4TStage = 0;
            ScreenWidth = 256;
            ScreenHeight = 192;
            BorderTopHeight = 48;// 55;// 48;
            BorderBottomHeight = 48;// 56;
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
