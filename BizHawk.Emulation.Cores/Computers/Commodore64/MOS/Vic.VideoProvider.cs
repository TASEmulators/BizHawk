using System.Drawing;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	public sealed partial class Vic : IVideoProvider
	{
	    [SaveState.DoNotSave] private static readonly int BgColor = Colors.ARGB(0, 0, 0);
        [SaveState.DoNotSave] private int[] _buf;
		[SaveState.DoNotSave] private int _bufHeight;
		[SaveState.DoNotSave] private int _bufLength;
		private int _bufOffset;
		[SaveState.DoNotSave] private int _bufWidth;
	    [SaveState.DoNotSave] private const int PixBufferSize = 24;
	    [SaveState.DoNotSave] private const int PixBorderBufferSize = 12;
	    private int[] _pixBuffer;
		private int _pixBufferIndex;
	    private int[] _pixBorderBuffer;
	    private int _pixBufferBorderIndex;

        // palette
        [SaveState.DoNotSave]
        private static readonly int[] Palette =
		{
				Colors.ARGB(0x00, 0x00, 0x00),
				Colors.ARGB(0xFF, 0xFF, 0xFF),
				Colors.ARGB(0x68, 0x37, 0x2B),
				Colors.ARGB(0x70, 0xA4, 0xB2),
				Colors.ARGB(0x6F, 0x3D, 0x86),
				Colors.ARGB(0x58, 0x8D, 0x43),
				Colors.ARGB(0x35, 0x28, 0x79),
				Colors.ARGB(0xB8, 0xC7, 0x6F),
				Colors.ARGB(0x6F, 0x4F, 0x25),
				Colors.ARGB(0x43, 0x39, 0x00),
				Colors.ARGB(0x9A, 0x67, 0x59),
				Colors.ARGB(0x44, 0x44, 0x44),
				Colors.ARGB(0x6C, 0x6C, 0x6C),
				Colors.ARGB(0x9A, 0xD2, 0x84),
				Colors.ARGB(0x6C, 0x5E, 0xB5),
				Colors.ARGB(0x95, 0x95, 0x95)
		};

        [SaveState.DoNotSave]
        public int BackgroundColor
		{
			get { return BgColor; }
		}

        [SaveState.DoNotSave]
        public int BufferHeight
		{
			get { return _bufHeight; }
		}

        [SaveState.DoNotSave]
        public int BufferWidth
		{
			get { return _bufWidth; }
		}

		public int[] GetVideoBuffer()
		{
			return _buf;
		}

	    [SaveState.DoNotSave]
	    public int VirtualWidth { get; private set; }

        [SaveState.DoNotSave]
        public int VirtualHeight { get; private set; }
	}
}
