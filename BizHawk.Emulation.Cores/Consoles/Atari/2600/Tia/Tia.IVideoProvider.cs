using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	public partial class TIA : IVideoProvider
	{
		public int[] GetVideoBuffer()
		{
			return _frameBuffer;
		}

		public int VirtualWidth
		{
			get
			{
				if (_pal)
				{
					return 320;
				}

				return 275; // 275 comes from NTSC specs and the actual pixel clock of a 2600 TIA
			}
		}

		public int VirtualHeight => BufferHeight;

		public int BufferWidth => ScreenWidth;

		public int BufferHeight
		{
			get
			{
				if (_pal)
				{
					return _core.Settings.PALBottomLine - _core.Settings.PALTopLine;
				}

				return _core.Settings.NTSCBottomLine - _core.Settings.NTSCTopLine;
			}
		}

		public int VsyncNumerator => _vsyncNum;

		public int VsyncDenominator => _vsyncDen;

		public int BackgroundColor => _core.Settings.BackgroundColor.ToArgb();

		private readonly int[] _frameBuffer = new int[ScreenWidth * MaxScreenHeight];
		private int _vsyncNum, _vsyncDen;
	}
}
