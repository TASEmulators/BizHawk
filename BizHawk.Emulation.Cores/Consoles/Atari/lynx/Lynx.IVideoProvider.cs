using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Lynx
{
	public partial class Lynx : IVideoProvider
	{
		private const int Width = 160;
		private const int Height = 102;

		private readonly int[] _videoBuff = new int[Width * Height];

		public int[] GetVideoBuffer() => _videoBuff;

		public int VirtualWidth => BufferWidth;

		public int VirtualHeight => BufferHeight;

		public int BufferWidth { get; }

		public int BufferHeight { get; }

		public int BackgroundColor => unchecked((int)0xff000000);

		public int VsyncNumerator => 16000000; // 16.00 mhz refclock

		public int VsyncDenominator => 16 * 105 * 159;
	}
}
