using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	public sealed partial class Vic : IVideoProvider
	{
		private static readonly int BgColor = Colors.ARGB(0, 0, 0);
		private int[] _buf;
		private int _bufHeight;
		private int _bufLength;
		private int _bufOffset;
		private int _bufWidth;

		private const int PixBufferSize = 24;
		private const int PixBorderBufferSize = 12;

		private int[] _pixBuffer;
		private int _pixBufferIndex;
		private int[] _pixBorderBuffer;
		private int _pixBufferBorderIndex;

		// palette
		// feos: these are the colors that come from pepto's final render at http://www.pepto.de/projects/colorvic/
		// these colors are also default at http://www.colodore.com/
		// colors from Vice's colodore.vpl, that were used here since the recent update, are somehow different
		// I'm using the colors from pepto's render. long term, this should have some adjustment options
		private static readonly int[] Palette =
		{
			Colors.ARGB(0x00, 0x00, 0x00),
			Colors.ARGB(0xff, 0xff, 0xff),
			Colors.ARGB(0x81, 0x33, 0x38),
			Colors.ARGB(0x75, 0xce, 0xc8),
			Colors.ARGB(0x8e, 0x3c, 0x97),
			Colors.ARGB(0x56, 0xac, 0x4d),
			Colors.ARGB(0x2e, 0x2c, 0x9b),
			Colors.ARGB(0xed, 0xf1, 0x71),
			Colors.ARGB(0x8e, 0x50, 0x29),
			Colors.ARGB(0x55, 0x38, 0x00),
			Colors.ARGB(0xc4, 0x6c, 0x71),
			Colors.ARGB(0x4a, 0x4a, 0x4a),
			Colors.ARGB(0x7b, 0x7b, 0x7b),
			Colors.ARGB(0xa9, 0xff, 0x9f),
			Colors.ARGB(0x70, 0x6d, 0xeb),
			Colors.ARGB(0xb2, 0xb2, 0xb2)
		};

		public int BackgroundColor => BgColor;

		public int BufferHeight => _bufHeight;

		public int BufferWidth => _bufWidth;

		public int[] GetVideoBuffer()
		{
			return _buf;
		}

		public int VirtualWidth { get; private set; }

		public int VirtualHeight { get; private set; }

		public int VsyncNumerator => CyclesPerSecond;

		public int VsyncDenominator => CyclesPerFrame;
	}
}
