using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	partial class DualNDS : IVideoProvider
	{
		public int VirtualWidth => 512;
		public int VirtualHeight => 384;
		public int BufferWidth => 512;
		public int BufferHeight => 384;

		public int VsyncNumerator => L.VsyncNumerator;

		public int VsyncDenominator => L.VsyncDenominator;

		public int BackgroundColor => unchecked((int)0xff000000);

		public int[] GetVideoBuffer() => VideoBuffer;

		private readonly int[] VideoBuffer = new int[256 * 2 * 384];
	}
}