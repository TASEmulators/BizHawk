
namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// +2A/+3 ULA
    /// </summary>
    class Screen128Plus2a : ULA
    {
        #region Construction

        public Screen128Plus2a(SpectrumBase machine)
			: base(machine)
        {
            // interrupt
            InterruptStartTime = 0;
            InterruptLength = 32;            
            // offsets
            RenderTableOffset = 58;
            ContentionOffset = 9;
            FloatingBusOffset = 0;
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
            BorderTopHeight = 48;// 55;
            BorderBottomHeight = 48; // 56;
            BorderLeftWidth = 48;
            BorderRightWidth = 48;
            ScanLineWidth = BorderLeftWidth + ScreenWidth + BorderRightWidth;

            RenderingTable = new RenderTable(this,
                MachineType.ZXSpectrum128Plus2a);

            SetupScreenSize();

            GenerateP3PortTable();
        }

        #endregion
    }
}
