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
		private static readonly int[] Palette =
		{
			Colors.ARGB(0x00, 0x00, 0x00),
			Colors.ARGB(0xFF, 0xFF, 0xFF),
			Colors.ARGB(0x96, 0x28, 0x2E),
			Colors.ARGB(0x5B, 0xD6, 0xCE),
			Colors.ARGB(0x9F, 0x2D, 0xAD),
			Colors.ARGB(0x41, 0xB9, 0x36),
			Colors.ARGB(0x27, 0x24, 0xC4),
			Colors.ARGB(0xEF, 0xF3, 0x47),
			Colors.ARGB(0x9F, 0x48, 0x15),
			Colors.ARGB(0x5E, 0x35, 0x00),
			Colors.ARGB(0xDA, 0x5F, 0x66),
			Colors.ARGB(0x47, 0x47, 0x47),
			Colors.ARGB(0x78, 0x78, 0x78),
			Colors.ARGB(0x91, 0xFF, 0x84),
			Colors.ARGB(0x68, 0x64, 0xFF),
			Colors.ARGB(0xAE, 0xAE, 0xAE)
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
