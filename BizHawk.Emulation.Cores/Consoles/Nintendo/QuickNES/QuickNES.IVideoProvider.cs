using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES
{
	public partial class QuickNES : IVideoProvider
	{
		public int BufferWidth { get; private set; }
		public int BufferHeight { get; private set; }
		public int BackgroundColor { get { return unchecked((int)0xff000000); } }

		public int[] GetVideoBuffer()
		{
			return VideoOutput;
		}

		public int VirtualWidth
		{
			get { return (int)(BufferWidth * 1.146); }
		}

		public int VirtualHeight
		{
			get { return BufferHeight; }
		}

		private int[] VideoOutput = new int[256 * 240];
		private int[] VideoPalette = new int[512];

		private int cropleft = 0;
		private int cropright = 0;
		private int croptop = 0;
		private int cropbottom = 0;

		private void RecalculateCrops()
		{
			cropright = cropleft = _settings.ClipLeftAndRight ? 8 : 0;
			cropbottom = croptop = _settings.ClipTopAndBottom ? 8 : 0;
			BufferWidth = 256 - cropleft - cropright;
			BufferHeight = 240 - croptop - cropbottom;
		}

		private void CalculatePalette()
		{
			for (int i = 0; i < 512; i++)
			{
				VideoPalette[i] =
					_settings.Palette[i * 3] << 16 |
					_settings.Palette[i * 3 + 1] << 8 |
					_settings.Palette[i * 3 + 2] |
					unchecked((int)0xff000000);
			}
		}

		private void Blit()
		{
			QN.qn_blit(Context, VideoOutput, VideoPalette, cropleft, croptop, cropright, cropbottom);
		}
	}
}
