namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	/// <summary>
	/// Video related functions
	/// </summary>
	public partial class ChannelF
	{
		private void BuildFrame1()
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
