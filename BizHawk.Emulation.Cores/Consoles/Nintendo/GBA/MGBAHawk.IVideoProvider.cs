using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class MGBAHawk : IVideoProvider
	{
		public int[] GetVideoBuffer()
		{
			return _videobuff;
		}

		public int VirtualWidth => 240;
		public int VirtualHeight => 160;

		public int BufferWidth => 240;
		public int BufferHeight => 160;

		public int BackgroundColor => unchecked((int)0xff000000);

		private readonly int[] _videobuff = new int[240 * 160];
	}
}
