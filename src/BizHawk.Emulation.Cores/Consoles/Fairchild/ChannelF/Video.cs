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
			/*
			0x101010, 0xFDFDFD, 0x5331FF, 0x5DCC02, 0xF33F4B, 0xE0E0E0, 0xA6FF91, 0xD0CEFF
			*/
			
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
				var rIndex = 128 * row;
				var c125 = (VRAM[rIndex + 125] & 0x03);
				var c126 = (VRAM[rIndex + 126] & 0x03);
				var pModifier = (((c126 & 0x02) | c125 >> 1) << 2);

				pModifier = ((VRAM[(row << 7) + 125] & 2) >> 1) | (VRAM[(row << 7) + 126] & 3);
				pModifier = (pModifier << 2) & 0xc;

				// columns
				for (int col = 0; col < 128; col++, counter++)
				{
					int cl = (VRAM[(row << 7) + col]) & 0x3;
					frameBuffer[(row << 7) + col] = CMap[pModifier | cl] & 0x7;
					//var nCol = pModifier + (VRAM[col | (row << 7)] & 0x03);
					//frameBuffer[counter] = FPalette[CMap[nCol]];
				}
			}
		}
	}
}
