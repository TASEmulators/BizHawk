using System.Drawing;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	public sealed partial class Vic : IVideoProvider
	{
        [SaveState.DoNotSave] private readonly int[] _buf;
		private readonly int _bufHeight;
		private readonly int _bufLength;
		private int _bufOffset;
		private readonly int _bufWidth;
	    private const int PixBufferSize = 12;
	    private int[] _pixBuffer;
		private int _pixBufferIndex;

		// palette
		private readonly int[] _palette =
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

		public int BackgroundColor
		{
			get { return Colors.ARGB(0, 0, 0); }
		}

		public int BufferHeight
		{
			get { return _bufHeight; }
		}

		public int BufferWidth
		{
			get { return _bufWidth; }
		}

		public int[] GetVideoBuffer()
		{
			return _buf;
		}

		public int VirtualWidth
		{
			get { return _bufWidth; }
		}

		public int VirtualHeight
		{
			get { return _bufHeight; }
		}
	}
}
