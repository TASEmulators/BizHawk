using System;

namespace BizHawk.Emulation.Consoles.GB
{
	public class GBColors
	{
		/*
		 * The GBC uses a RGB555 color space, but it most definately does not resemble sRGB at all.
		 * To make matters worse, because of the reflective screen, the visible colors depend greatly
		 * on the viewing environment.
		 * 
		 * All of these algorithms convert from GBC RGB555 to sRGB RGB888
		 */
		public struct Triple
		{
			public int r;
			public int g;
			public int b;
			public Triple(int r, int g, int b)
			{
				this.r = r;
				this.g = g;
				this.b = b;
			}
			
			public Triple Bit5to8Bad()
			{
				Triple ret;
				ret.r = r * 8;
				ret.g = g * 8;
				ret.b = b * 8;
				return ret;
			}

			public Triple Bit5to8Good()
			{
				Triple ret;
				ret.r = (r * 255 + 15) / 31;
				ret.g = (g * 255 + 15) / 31;
				ret.b = (b * 255 + 15) / 31;
				return ret;
			}

			public int ToARGB32()
			{
				return b | g << 8 | r << 16 | 255 << 24;
			}
		}

		// the version of gambatte in bizhawk
		public static Triple GambatteColor(Triple c)
		{
			Triple ret;
			ret.r = (c.r * 13 + c.g * 2 + c.b) >> 1;
			ret.g = (c.g * 3 + c.b) << 1;
			ret.b = (c.r * 3 + c.g * 2 + c.b * 11) >> 1;
			return ret;
		}

		// vba's default mode
		public static Triple VividVBAColor(Triple c)
		{
			return c.Bit5to8Bad();
		}

		// "gameboy colors" mode on older versions of VBA
		static int gbGetValue(int min, int max, int v)
		{
			return (int)(min + (float)(max - min) * (2.0 * (v / 31.0) - (v / 31.0) * (v / 31.0)));
		}
		public static Triple OldVBAColor(Triple c)
		{
			Triple ret;
			ret.r = gbGetValue(gbGetValue(4, 14, c.g),
				gbGetValue(24, 29, c.g), c.r) - 4;
			ret.g = gbGetValue(gbGetValue(4 + gbGetValue(0, 5, c.r),
				14 + gbGetValue(0, 3, c.r), c.b),
				gbGetValue(24 + gbGetValue(0, 3, c.r),
				29 + gbGetValue(0, 1, c.r), c.b), c.g) - 4;
			ret.b = gbGetValue(gbGetValue(4 + gbGetValue(0, 5, c.r),
				14 + gbGetValue(0, 3, c.r), c.g),
				gbGetValue(24 + gbGetValue(0, 3, c.r),
				29 + gbGetValue(0, 1, c.r), c.g), c.b) - 4;
			return ret.Bit5to8Bad();
		}

		// "gameboy colors" mode on newer versions of VBA
		public static Triple NewVBAColor(Triple c)
		{
			Triple ret;
			ret.r = (c.r * 13 + c.g * 2 + c.b * 1 + 8) >> 4;
			ret.g = (c.r * 1 + c.g * 12 + c.b * 3 + 8) >> 4;
			ret.b = (c.r * 2 + c.g * 2 + c.b * 12 + 8) >> 4;
			return ret.Bit5to8Bad();
		}

		// as vivid as possible
		public static Triple UltraVividColor(Triple c)
		{
			return c.Bit5to8Good();
		}

		// possibly represents a GBA screen, more or less
		// but probably not (black point?)
		static int GBAGamma(int input)
		{
			return (int)Math.Round(Math.Pow(input / 31.0, 3.5 / 2.2) * 255.0);
		}
		public static Triple GBAColor(Triple c)
		{
			Triple ret;
			ret.r = GBAGamma(c.r);
			ret.g = GBAGamma(c.g);
			ret.b = GBAGamma(c.b);
			return ret;
		}

		public enum ColorType
		{
			gambatte,
			vivid,
			vbavivid,
			vbagbnew,
			vbabgbold,
			gba
		};

		public static int[] GetLut(ColorType c)
		{
			Func<Triple, Triple> f = null;
			switch (c)
			{
				case ColorType.gambatte: f = GambatteColor; break;
				case ColorType.vivid: f = UltraVividColor; break;
				case ColorType.vbavivid: f = VividVBAColor; break;
				case ColorType.vbagbnew: f = NewVBAColor; break;
				case ColorType.vbabgbold: f = OldVBAColor; break;
				case ColorType.gba: f = GBAColor; break;
			}
			int[] ret = new int[32768];
			int i = 0;
			for (int b = 0; b < 32; b++)
				for (int g = 0; g < 32; g++)
					for (int r = 0; r < 32; r++)
						ret[i++] = f(new Triple(r, g, b)).ToARGB32();
			return ret;
		}
	}
}
