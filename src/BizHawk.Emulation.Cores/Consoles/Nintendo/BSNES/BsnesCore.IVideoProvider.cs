using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.BSNES
{
	public partial class BsnesCore : IVideoProvider
	{
		public int VirtualWidth
		{
			get
			{
				double virtualWidth = BufferWidth * PixelAspectRatio;
				if (!_isSGB && BufferWidth == 256 && (BufferHeight > 240 || _settings.AlwaysDoubleSize)) virtualWidth *= 2;

				return (int)Math.Round(virtualWidth);
			}
		}

		public int VirtualHeight => !_isSGB && BufferHeight <= 240 && (BufferWidth > 256 || _settings.AlwaysDoubleSize) ? BufferHeight * 2 : BufferHeight;

		public int BufferWidth { get; private set; } = 256;

		public int BufferHeight { get; private set; } = 224;

		public int BackgroundColor => 0;

		public int[] GetVideoBuffer() => _videoBuffer;

		public int VsyncNumerator { get; }
		public int VsyncDenominator { get; }

		private int[] _videoBuffer = new int[256 * 224];

		private const double NTSCPixelAspectRatio = 8D / 7;
		private const double PALPixelAspectRatio = 59 * 125000 / (165 * 64489 / 2D);
		private double PixelAspectRatio => _settings.AspectRatioCorrection switch
		{
			BsnesApi.ASPECT_RATIO_CORRECTION.Auto => this._region == BsnesApi.SNES_REGION.NTSC ? NTSCPixelAspectRatio : PALPixelAspectRatio,
			BsnesApi.ASPECT_RATIO_CORRECTION.NTSC => NTSCPixelAspectRatio,
			BsnesApi.ASPECT_RATIO_CORRECTION.PAL => PALPixelAspectRatio,
			_ => 1
		};
	}
}
