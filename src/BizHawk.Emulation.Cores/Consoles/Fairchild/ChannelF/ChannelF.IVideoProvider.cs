using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	public partial class ChannelF : IVideoProvider, IRegionable
	{
		/// <summary>
		/// 128x64 pixels - 8192x2bits (2 KB)
		/// For the purposes of this core we will use 8192 bytes and just &amp; 0x03
		/// </summary>
		public byte[] VRAM = new byte[(128 * 64)];


		public static readonly int[] FPalette =
		{
			//0x101010, 0xFDFDFD, 0x5331FF, 0x5DCC02, 0xF33F4B, 0xE0E0E0, 0xA6FF91, 0xD0CEFF
			
			Colors.ARGB(0x10, 0x10, 0x10),		// Black
			Colors.ARGB(0xFD, 0xFD, 0xFD),		// White
			Colors.ARGB(0xFF, 0x31, 0x53),		// Red
			Colors.ARGB(0x02, 0xCC, 0x5D),		// Green
			Colors.ARGB(0x4B, 0x3F, 0xF3),		// Blue
			Colors.ARGB(0xE0, 0xE0, 0xE0),		// Gray
			Colors.ARGB(0x91, 0xFF, 0xA6),		// BGreen
			Colors.ARGB(0xCE, 0xD0, 0xFF),		// BBlue
			
		};

		public static readonly int[] CMap =
		{
			0, 1, 1, 1,
			7, 4, 2, 3,
			5, 4, 2, 3,
			6, 4, 2, 3,
		};

		private int latch_colour = 2; //2;
		private int latch_x;
		private int latch_y;

		private readonly int[] frameBuffer = new int[128 * 64];

		private void BuildFrame()
		{
			for (int r = 0; r < 64; r++)
			{
				// lines
				var p1 = (VRAM[(r * 0x80) + 125]) & 0x03;
				var p2 = (VRAM[(r * 0x80) + 126]) & 0x03;
				var pOffset = ((p2 & 0x02) | (p1 >> 1)) << 2;

				for (int c = 0; c < 128; c++)
				{
					// columns
					var colourIndex = pOffset + (VRAM[c | (r << 7)] & 0x03);
					frameBuffer[(r << 7) + c] = CMap[colourIndex];
				}
			}
		}


		//public int _frameHz => region == RegionType.NTSC ? 60 : 50;
		public int[] CroppedBuffer = new int[102 * 58];
		public int VirtualWidth => BufferWidth * 4;
		public int VirtualHeight => (int)(BufferHeight * 1.43) * 4;
		public int BufferWidth => 102; //128
		public int BufferHeight => 58; //64
		public int BackgroundColor => Colors.ARGB(0xFF, 0xFF, 0xFF);
		public int VsyncNumerator => (int)refreshRate;
		public int VsyncDenominator => 1;

		public int[] GetVideoBuffer()
		{
			BuildFrame();

			var lBorderWidth = 4;
			var rBorderWidth = 128 - 102 - lBorderWidth;
			var tBorderHeight = 4;
			var bBorderHeight = 64 - 58 - tBorderHeight;
			var startP = 128 * tBorderHeight;
			var endP = 128 * bBorderHeight;

			int index = 0;

			for (int i = startP; i < frameBuffer.Length - endP; i += 128)
			{
				for (int p = lBorderWidth; p < 128 - rBorderWidth; p++)
				{
					if (index == CroppedBuffer.Length)
						break;

					CroppedBuffer[index++] = FPalette[frameBuffer[i + p]];
				}
			}

			return CroppedBuffer;
		}

		public DisplayType Region => region == RegionType.NTSC ? DisplayType.NTSC : DisplayType.PAL;
	}
}
