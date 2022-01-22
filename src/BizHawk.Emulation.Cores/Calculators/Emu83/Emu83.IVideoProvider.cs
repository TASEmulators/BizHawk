using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Calculators.Emu83
{
	public partial class Emu83 : IVideoProvider
	{
		public int VirtualWidth => 96;
		public int VirtualHeight => 64;
		public int BufferWidth => 96;
		public int BufferHeight => 64;
		public int BackgroundColor => 0;
		public int VsyncNumerator => NullVideo.DefaultVsyncNum;
		public int VsyncDenominator => NullVideo.DefaultVsyncDen;

		private readonly int[] _videoBuffer = new int[96 * 64];
		public int[] GetVideoBuffer() => _videoBuffer;
	}
}
