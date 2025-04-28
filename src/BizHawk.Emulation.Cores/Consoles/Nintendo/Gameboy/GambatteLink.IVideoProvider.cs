using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class GambatteLink : IVideoProvider
	{
		public int VirtualWidth => 160 * _numCores;
		public int VirtualHeight => 144;
		public int BufferWidth => 160 * _numCores;
		public int BufferHeight => 144;

		public int VsyncNumerator => _linkedCores[P1].VsyncNumerator;

		public int VsyncDenominator => _linkedCores[P1].VsyncDenominator;

		public int BackgroundColor => unchecked((int)0xff000000);

		private readonly int[] FrameBuffer;

		public int[] GetVideoBuffer() => VideoBuffer;

		private readonly int[] VideoBuffer;

		private int[] CreateVideoBuffer()
		{
			var b = new int[BufferWidth * BufferHeight];
			for (int i = 0; i < b.Length; i++)
			{
				b[i] = -1; // GB/C screen is disabled on bootup, so it always starts as white, not black
			}
			return b;
		}

	}
}
