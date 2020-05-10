using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Calculators
{
	public partial class TI83 : IVideoProvider
	{
		public int VirtualWidth => 96;
		public int VirtualHeight => 64;
		public int BufferWidth => 96;
		public int BufferHeight => 64;
		public int BackgroundColor => 0;
		public int VsyncNumerator => NullVideo.DefaultVsyncNum;
		public int VsyncDenominator => NullVideo.DefaultVsyncDen;

		public int[] GetVideoBuffer()
		{
			// unflatten bit buffer
			int[] pixels = new int[96 * 64];
			int i = 0;
			for (int y = 0; y < 64; y++)
			{
				for (int x = 0; x < 96; x++)
				{
					int offset = (y * 96) + x;
					int buffByte = offset >> 3;
					int buffBit = offset & 7;
					int bit = (_vram[buffByte] >> (7 - buffBit)) & 1;
					if (bit == 0)
					{
						unchecked
						{
							pixels[i++] = (int)_settings.BGColor;
						}
					}
					else
					{
						pixels[i++] = (int)_settings.ForeColor;
					}
				}
			}

			return pixels;
		}
	}
}
