using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Sameboy
{
	public partial class Sameboy : IVideoProvider
	{
		private readonly int[] VideoBuffer = CreateVideoBuffer();

		private static int[] CreateVideoBuffer()
		{
			var b = new int[256 * 224];
			for (int i = 0; i < (256 * 224); i++)
			{
				b[i] = -1;
			}
			return b;
		}

		public int[] GetVideoBuffer()
		{
			return VideoBuffer;
		}

		public int VirtualWidth => _settings.ShowBorder ? 256 : 160;

		public int VirtualHeight => _settings.ShowBorder ? 224 : 144;

		public int BufferWidth => _settings.ShowBorder ? 256 : 160;

		public int BufferHeight => _settings.ShowBorder ? 224 : 144;

		public int BackgroundColor => 0;

		public int VsyncNumerator => 262144;

		public int VsyncDenominator => 4389;
	}
}
