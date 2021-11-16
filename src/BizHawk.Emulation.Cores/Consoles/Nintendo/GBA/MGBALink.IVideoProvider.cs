using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class MGBALink : IVideoProvider
	{
		public int VirtualWidth => 240 * _numCores;
		public int VirtualHeight => 160;

		public int BufferWidth => 240 * _numCores;
		public int BufferHeight => 160;

		public int VsyncNumerator => _linkedCores[P1].VsyncNumerator;

		public int VsyncDenominator => _linkedCores[P1].VsyncDenominator;

		public int BackgroundColor => unchecked((int)0xff000000);

		public int[] GetVideoBuffer() => _videobuff;

		private readonly int[] _videobuff;
	}
}
