namespace BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES
{
	public static class Nes_NTSC_Colors
	{
		// just the color deemphasis routines from nes_ntsc

		private static void RGB_TO_YIQ(float r, float g, float b, out float y, out float i, out float q)
		{
			y = (r) * 0.299f + (g) * 0.587f + (b) * 0.114f;
			i = (r) * 0.596f - (g) * 0.275f - (b) * 0.321f;
			q = (r) * 0.212f - (g) * 0.523f + (b) * 0.311f;
		}

		private static readonly float[] to_rgb = { 0.956f, 0.621f, -0.272f, -0.647f, -1.105f, 1.702f };

		private static void YIQ_TO_RGB(float y, float i, float q, out float r, out float g, out float b)
		{
			r = (float)(y + to_rgb[0] * i + to_rgb[1] * q);
			g = (float)(y + to_rgb[2] * i + to_rgb[3] * q);
			b = (float)(y + to_rgb[4] * i + to_rgb[5] * q);
		}

		private static readonly float[] lo_levels = { -0.12f, 0.00f, 0.31f, 0.72f };
		private static readonly float[] hi_levels = { 0.40f, 0.68f, 1.00f, 1.00f };
		private static readonly byte[] tints = { 0, 6, 10, 8, 2, 4, 0, 0 };

		private static readonly float[] phases =
		{
			-1.0f, -0.866025f, -0.5f, 0.0f,  0.5f,  0.866025f,
			 1.0f,  0.866025f,  0.5f, 0.0f, -0.5f, -0.866025f,
			-1.0f, -0.866025f, -0.5f, 0.0f,  0.5f,  0.866025f,
			 1.0f
		};

		public static void Emphasis(byte[] inp, byte[] outp, int entrynum)
		{
			int level = entrynum >> 4 & 0x03;
			float lo = lo_levels[level];
			float hi = hi_levels[level];

			int color = entrynum & 0x0f;
			int tint = entrynum >> 6;

			if (color == 0)
				lo = hi;
			if (color == 0x0D)
				hi = lo;
			if (color > 0x0D)
				hi = lo = 0.0f;

			const float to_float = 1.0f / 0xff;
			float r = to_float * inp[0];
			float g = to_float * inp[1];
			float b = to_float * inp[2];

			RGB_TO_YIQ(r, g, b, out float y, out var i, out var q);

			if (tint > 0 && color < 0x0d)
			{
				const float atten_mul = 0.79399f;
				const float atten_sub = 0.0782838f;

				if (tint == 7)
				{
					y = y * (atten_mul * 1.13f) - (atten_sub * 1.13f);
				}
				else
				{
					int tint_color = tints[tint];
					float sat = hi * (0.5f - atten_mul * 0.5f) + atten_sub * 0.5f;
					y -= sat * 0.5f;
					if (tint >= 3 && tint != 4)
					{
						/* combined tint bits */
						sat *= 0.6f;
						y -= sat;
					}
					i += phases[tint_color] * sat;
					q += phases[tint_color + 3] * sat;
				}

			}

			YIQ_TO_RGB(y, i, q, out r, out g, out b);
			r = Math.Min(1.0f, Math.Max(0.0f, r));
			g = Math.Min(1.0f, Math.Max(0.0f, g));
			b = Math.Min(1.0f, Math.Max(0.0f, b));

			outp[0] = (byte)Math.Round(r * 255);
			outp[1] = (byte)Math.Round(g * 255);
			outp[2] = (byte)Math.Round(b * 255);
		}
	}
}
