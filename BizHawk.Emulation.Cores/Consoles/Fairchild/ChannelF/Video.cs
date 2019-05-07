using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	/// <summary>
	/// Video related functions
	/// </summary>
	public partial class ChannelF
	{
		/// <summary>
		/// 128x64 pixels - 8192x2bits (2 KB)
		/// For the purposes of this core we will use 8192 bytes and just & 0x03
		/// </summary>
		public byte[] VRAM = new byte[(128 * 64)];

		public static readonly int[] FPalette =
		{
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

		private int _colour = 2;
		private int _x;
		private int _y;
		private int _arm;

		private int[] frameBuffer = new int[128 * 64];

		private void BuildFrame()
		{
			// rows
			int counter = 0;
			for (int row = 0; row < 64; row++)
			{
				// columns 125 and 126 hold the palette index modifier for the entire row
				var rIndex = 64 * row;
				var c125 = (VRAM[rIndex + 125] & 0x02) >> 1;
				var c126 = (VRAM[rIndex + 126] & 0x03);
				var pModifier = ((c125 | c126) << 2) & 0x0C;

				// columns
				for (int col = 0; col < 128; col++, counter++)
				{
					int colour = (VRAM[rIndex + col]) & 0x03;
					var finalColorIndex = pModifier | colour;
					var paletteLookup = CMap[finalColorIndex & 0x0f] & 0x07;
					frameBuffer[counter] = FPalette[paletteLookup];
				}
			}
		}

		private void BuildFrame1()
		{
			int cnt = 0;
			// rows
			for (int row = 0; row < 64; row++)
			{
				var yIndex = row * 128;
				var yByte = yIndex / 4;

				// last byte for this row contains palette modifier
				var pModifier = (byte)(VRAM[yByte + 31] & 0x0C);

				// columns
				for (int col = 0; col < 128; col++)
				{
					var fbIndex = (row * 64) + col;

					var xByte = col / 4;
					var xRem = col % 4;
					var xyByte = yByte + xByte;

					// each byte contains 4 pixel colour values, b0b1, b2b3, b4b5, b6b7
					int colour = 0;

					switch (xRem)
					{
						case 0:
							colour = VRAM[xyByte] & 0x03;
							break;
						case 1:
							colour = VRAM[xyByte] & 0x0C;
							break;
						case 2:
							colour = VRAM[xyByte] & 0x30;
							break;
						case 3:
							colour = VRAM[xyByte] & 0xC0;
							break;
					}

					var finalColorIndex = pModifier | colour;
					var paletteLookup = CMap[finalColorIndex & 0x0f] & 0x07;
					frameBuffer[fbIndex] = FPalette[paletteLookup];

					cnt++;
				}
			}
		}
	}
}
