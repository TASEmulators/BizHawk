using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Calculators
{
	public partial class TI83  : IVideoProvider
	{
		public int VirtualWidth{ get { return 96; } }
		public int VirtualHeight { get { return 64; } }
		public int BufferWidth { get { return 96; } }
		public int BufferHeight { get { return 64; } }
		public int BackgroundColor { get { return 0; } }

		public int[] GetVideoBuffer()
		{
			// unflatten bit buffer
			int[] pixels = new int[96 * 64];
			int i = 0;
			for (int y = 0; y < 64; y++)
			{
				for (int x = 0; x < 96; x++)
				{
					int offset = y * 96 + x;
					int bufbyte = offset >> 3;
					int bufbit = offset & 7;
					int bit = ((_vram[bufbyte] >> (7 - bufbit)) & 1);
					if (bit == 0)
					{
						unchecked { pixels[i++] = (int)Settings.BGColor; }
					}
					else
					{
						pixels[i++] = (int)Settings.ForeColor;
					}

				}
			}

			return pixels;
		}
	}
}
