namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public static class GBColors
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

			private static readonly int[] sameboy_cgb_color_curve = new int[32] { 0, 6, 12, 20, 28, 36, 45, 56, 66, 76, 88, 100, 113, 125, 137, 149, 161, 172, 182, 192, 202, 210, 218, 225, 232, 238, 243, 247, 250, 252, 254, 255 };
			private static readonly int[] sameboy_agb_color_curve = new int[32] { 0, 3, 8, 14, 20, 26, 33, 40, 47, 54, 62, 70, 78, 86, 94, 103, 112, 120, 129, 138, 147, 157, 166, 176, 185, 195, 205, 215, 225, 235, 245, 255 };
			private static readonly int[] sameboy_sgb_color_curve = new int[32] { 0, 2, 5, 9, 15, 20, 27, 34, 42, 50, 58, 67, 76, 85, 94, 104, 114, 123, 133, 143, 153, 163, 173, 182, 192, 202, 211, 220, 229, 238, 247, 255 };

			public Triple Bit5to8SameBoy(bool sgb, bool agb)
			{
				Triple ret;
				if (sgb)
				{
					ret.r = sameboy_sgb_color_curve[r];
					ret.g = sameboy_sgb_color_curve[g];
					ret.b = sameboy_sgb_color_curve[b];
				}
				else if (agb)
				{
					ret.r = sameboy_agb_color_curve[r];
					ret.g = sameboy_agb_color_curve[g];
					ret.b = sameboy_agb_color_curve[b];
				}
				else
				{
					ret.r = sameboy_cgb_color_curve[r];
					ret.g = sameboy_cgb_color_curve[g];
					ret.b = sameboy_cgb_color_curve[b];
				}

				return ret;
			}

			public int ToARGB32()
			{
				return b | g << 8 | r << 16 | 255 << 24;
			}
		}

		// sameboy's "emulate hardware" color conversion
		// this is probably the most "accurate" conversion we have
		// note: this differs based on sgb / agb being emulated
		// todo: maybe add in its "harsh reality" too? (""accuracy"")
		public static Triple SameBoyColor(Triple c, bool sgb, bool agb)
		{
			Triple ret = c.Bit5to8SameBoy(sgb, agb);
			if (!sgb)
			{
				if (agb)
				{
					ret.g = (ret.g * 6 + ret.b) / 7;
				}
				else
				{
					ret.g = (ret.g * 3 + ret.b) / 4;
				}
			}

			return ret;
		}

		// the version of gambatte in bizhawk
		public static Triple GambatteColor(Triple c, bool sgb, bool agb)
		{
			Triple ret;
			ret.r = (c.r * 13 + c.g * 2 + c.b) >> 1;
			ret.g = (c.g * 3 + c.b) << 1;
			ret.b = (c.r * 3 + c.g * 2 + c.b * 11) >> 1;
			return ret;
		}

		public static Triple LibretroGBCColor(Triple c, bool sgb, bool agb)
		{
			Triple ret;
			double gammaR = Math.Pow((double)c.r / 31, 2.2);
			double gammaG = Math.Pow((double)c.g / 31, 2.2);
			double gammaB = Math.Pow((double)c.b / 31, 2.2);
			ret.r = (int)(Math.Pow(gammaR * .87 + gammaG * .18 - gammaB * .05, 1 / 2.2) * 255 + .5);
			ret.g = (int)(Math.Pow(gammaG * .66 + gammaR * .115 + gammaB * .225, 1 / 2.2) * 255 + .5);
			ret.b = (int)(Math.Pow(gammaB * .79 + gammaR * .14 + gammaG * .07, 1 / 2.2) * 255 + .5);
			ret.r = Math.Max(0, Math.Min(255, ret.r));
			ret.g = Math.Max(0, Math.Min(255, ret.g));
			ret.b = Math.Max(0, Math.Min(255, ret.b));
			return ret;
		}

		// vba's default mode
		public static Triple VividVBAColor(Triple c, bool sgb, bool agb)
		{
			return c.Bit5to8Bad();
		}

		// "gameboy colors" mode on older versions of VBA
		private static int gbGetValue(int min, int max, int v)
		{
			return (int)(min + (max - min) * (2.0 * (v / 31.0) - (v / 31.0) * (v / 31.0)));
		}

		public static Triple OldVBAColor(Triple c, bool sgb, bool agb)
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
		public static Triple NewVBAColor(Triple c, bool sgb, bool agb)
		{
			Triple ret;
			ret.r = (c.r * 13 + c.g * 2 + c.b * 1 + 8) >> 4;
			ret.g = (c.r * 1 + c.g * 12 + c.b * 3 + 8) >> 4;
			ret.b = (c.r * 2 + c.g * 2 + c.b * 12 + 8) >> 4;
			return ret.Bit5to8Bad();
		}

		// as vivid as possible
		public static Triple UltraVividColor(Triple c, bool sgb, bool agb)
		{
			return c.Bit5to8Good();
		}

		// possibly represents a GBA screen, more or less
		// but probably not (black point?)
		private static int GBAGamma(int input)
		{
			return (int)Math.Round(Math.Pow(input / 31.0, 3.5 / 2.2) * 255.0);
		}

		public static Triple GBAColor(Triple c, bool sgb, bool agb)
		{
			Triple ret;
			ret.r = GBAGamma(c.r);
			ret.g = GBAGamma(c.g);
			ret.b = GBAGamma(c.b);
			return ret;
		}

		public enum ColorType
		{
			sameboy,
			gambatte,
			vivid,
			vbavivid,
			vbagbnew,
			vbabgbold,
			gba,
			libretrogbc,
		}

		public static int[] GetLut(ColorType c, bool sgb = false, bool agb = false)
		{
			int[] ret = new int[32768];
			GetLut(c, ret, sgb, agb);
			return ret;
		}

		public static void GetLut(ColorType c, int[] dest, bool sgb = false, bool agb = false)
		{
			Func<Triple, bool, bool, Triple> f = c switch
			{
				ColorType.sameboy => SameBoyColor,
				ColorType.gambatte => GambatteColor,
				ColorType.vivid => UltraVividColor,
				ColorType.vbavivid => VividVBAColor,
				ColorType.vbagbnew => NewVBAColor,
				ColorType.vbabgbold => OldVBAColor,
				ColorType.gba => GBAColor,
				ColorType.libretrogbc => LibretroGBCColor,
				_ => throw new InvalidOperationException()
			};

			int i = 0;
			for (int b = 0; b < 32; b++)
				for (int g = 0; g < 32; g++)
					for (int r = 0; r < 32; r++)
						dest[i++] = f(new Triple(r, g, b), sgb, agb).ToARGB32();
		}
	}
}
