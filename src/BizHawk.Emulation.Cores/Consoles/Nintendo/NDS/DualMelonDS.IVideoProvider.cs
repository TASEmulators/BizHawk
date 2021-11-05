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

		private unsafe void ProcessVideo()
		{
			fixed (int* lb = &L.GetVideoBuffer()[0], rb = &R.GetVideoBuffer()[0], vb = &VideoBuffer[0])
			{
				for (int i = 0; i < 384; i++)
				{
					for (int j = 0; j < 256; j++)
					{
						vb[i * 512 + j] = lb[i * 256 + j];
						vb[i * 512 + j + 256] = rb[i * 256 + j];
					}
				}
			}
		}
	}
}