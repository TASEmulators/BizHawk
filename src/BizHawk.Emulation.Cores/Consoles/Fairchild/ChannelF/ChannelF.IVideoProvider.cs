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

		private int scanlineRepeats;
		private int[] frameBuffer;
		private int[] outputBuffer;

		public void SetupVideo()
		{
			scanlineRepeats = Region == DisplayType.NTSC ? 4 : 5;
			frameBuffer = new int[128 * 64];
			outputBuffer = new int[128 * 2 * 64 * scanlineRepeats];
		}

		private void BuildFrameFromRAM()
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

		private void ExpandFrame()
		{
			int initialWidth = 128;
			int initialHeight = 64;

			for (int lines = 0; lines < initialHeight; lines++)
			{
				for (int i = 0; i < scanlineRepeats; i++)
				{
					for (int x = 0; x < initialWidth; x++)
					{
						for (int j = 0; j < 2; j++)
						{
							outputBuffer[(lines * scanlineRepeats + i) * initialWidth * 2 + x * 2 + j] = FPalette[frameBuffer[lines * initialWidth + x]];
						}
					}
				}
			}

		}

		public int lBorder => 8;
		public int rBorder => 52;
		public int tBorder => 8;
		public int bBorder => 4;

		public int VirtualWidth => (int)((BufferWidth - lBorder - rBorder) * 2.2); 
		public int VirtualHeight => Region == DisplayType.NTSC ? BufferHeight - tBorder - bBorder : (int)((BufferHeight - tBorder - bBorder) * 0.8); 
		public int BufferWidth => 256 - lBorder - rBorder;
		public int BufferHeight => (64 * scanlineRepeats) - tBorder - bBorder;
		public int BackgroundColor => Colors.ARGB(0xFF, 0xFF, 0xFF);
		public int VsyncNumerator => (int)refreshRate;
		public int VsyncDenominator => 1;

		public int[] TrimOutputBuffer(int[] buff, int leftTrim, int topTrim, int rightTrim, int bottomTrim)
		{
			int initialWidth = 128 * 2;
			int initialHeight = 64 * scanlineRepeats;
			int newWidth = initialWidth - leftTrim - rightTrim;
			int newHeight = initialHeight - topTrim - bottomTrim;

			int[] trimmedBuffer = new int[newWidth * newHeight];

			for (int y = 0; y < newHeight; y++)
			{
				for (int x = 0; x < newWidth; x++)
				{
					trimmedBuffer[y * newWidth + x] = buff[(y + topTrim) * initialWidth + (x + leftTrim)];
				}
			}

			return trimmedBuffer;
		}


		public int[] GetVideoBuffer()
		{
			BuildFrameFromRAM();
			ExpandFrame();
			return TrimOutputBuffer(outputBuffer, lBorder, tBorder, rBorder, bBorder);
		}

		public DisplayType Region => region == RegionType.NTSC ? DisplayType.NTSC : DisplayType.PAL;
	}
}
