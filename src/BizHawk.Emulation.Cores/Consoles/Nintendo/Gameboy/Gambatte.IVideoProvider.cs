using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class Gameboy : IVideoProvider
	{
		/// <summary>
		/// buffer of last frame produced
		/// </summary>
		private readonly int[] FrameBuffer = CreateVideoBuffer();

		/// <summary>
		/// stored image of most recent frame
		/// </summary>
		private readonly int[] VideoBuffer = CreateVideoBuffer();

		/// <summary>
		/// stored image of most recent sgb frame
		/// </summary>
		private readonly int[] SgbVideoBuffer = new int[256 * 244];

		private static int[] CreateVideoBuffer()
		{
			var b = new int[160 * 144];
			for (var i = 0; i < (160 * 144); i++)
			{
				b[i] = -1; // GB/C screen is disabled on bootup, so it always starts as white, not black
			}
			return b;
		}

		public int[] GetVideoBuffer()
			=> IsSgb && _settings.ShowBorder ? SgbVideoBuffer : VideoBuffer;

		public int VirtualWidth => IsSgb && _settings.ShowBorder ? 256 : 160;

		public int VirtualHeight => IsSgb && _settings.ShowBorder ? 224 : 144;

		public int BufferWidth => IsSgb && _settings.ShowBorder ? 256 : 160;

		public int BufferHeight => IsSgb && _settings.ShowBorder ? 224 : 144;

		public int BackgroundColor => 0;

		public int VsyncNumerator => 262144;

		public int VsyncDenominator => 4389;
	}
}
