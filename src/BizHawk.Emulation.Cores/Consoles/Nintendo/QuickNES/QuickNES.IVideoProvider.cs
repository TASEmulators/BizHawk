using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES
{
	public partial class QuickNES : IVideoProvider
	{
		public int BufferWidth { get; private set; }
		public int BufferHeight { get; private set; }
		public int BackgroundColor => unchecked((int)0xff000000);

		public int VsyncNumerator => 39375000;
		public int VsyncDenominator => 655171;

		public int[] GetVideoBuffer() =>_videoOutput;

		public int VirtualWidth => (int)(BufferWidth * 1.146);

		public int VirtualHeight => BufferHeight;

		private readonly int[] _videoOutput = new int[256 * 240];
		private readonly int[] _videoPalette = new int[512];

		private int _cropLeft, _cropRight, _cropTop, _cropBottom;

		private void RecalculateCrops()
		{
			_cropRight = _cropLeft = _settings.ClipLeftAndRight ? 8 : 0;
			_cropBottom = _cropTop = _settings.ClipTopAndBottom ? 8 : 0;
			BufferWidth = 256 - _cropLeft - _cropRight;
			BufferHeight = 240 - _cropTop - _cropBottom;
		}

		private void CalculatePalette()
		{
			for (int i = 0; i < 512; i++)
			{
				_videoPalette[i] =
					_settings.Palette[i * 3] << 16 |
					_settings.Palette[i * 3 + 1] << 8 |
					_settings.Palette[i * 3 + 2] |
					unchecked((int)0xff000000);
			}
		}

		private void Blit()
		{
			QN.qn_blit(Context, _videoOutput, _videoPalette, _cropLeft, _cropTop, _cropRight, _cropBottom);
		}
	}
}
