namespace BizHawk.Emulation.Consoles.Nintendo
{
	partial class NES
	{
		public static class Palettes
		{
			static float[] rtmul = { 1.239f, 0.794f, 1.019f, 0.905f, 1.023f, 0.741f, 0.75f };
			static float[] gtmul = { 0.915f, 1.086f, 0.98f, 1.026f, 0.908f, 0.987f, 0.75f };
			static float[] btmul = { 0.743f, 0.882f, 0.653f, 1.277f, 0.979f, 0.101f, 0.75f };

			public static void ApplyDeemphasis(ref int r, ref int g, ref int b, int deemph_bits)
			{
				//DEEMPH BITS MAY BE ORDERED WRONG. PLEASE CHECK
				if (deemph_bits == 0) return;
				int d = deemph_bits-1;
				r = (int)(r * rtmul[d]);
				g = (int)(g * gtmul[d]);
				b = (int)(b * btmul[d]);
				if (r > 0xFF) r = 0xFF;
				if (g > 0xFF) g = 0xFF;
				if (b > 0xFF) b = 0xFF;
			}

			/// <summary>
			/// Loads a simple 192 byte (64 entry RGB888) palette which is FCEUX format (and probably other emulators as well)
			/// </summary>
			/// <param name="fileContents">192 bytes, the contents of the palette file</param>
			public static int[,] Load_FCEUX_Palette(byte[] fileContents)
			{
				if (fileContents.Length != 192) return null;
				int[,] ret = new int[64, 3];
				int i=0;
				for (int c = 0; c < 64; c++)
				{
					for(int z=0;z<3;z++)
						ret[c,z] = fileContents[i++];
				}
				return ret;
			}

			const int SHIFT = 2;
			public static int[,] FCEUX_Standard = new int[,]
			{
				{ 0x1D<<SHIFT, 0x1D<<SHIFT, 0x1D<<SHIFT }, /* Value 0 */
				{ 0x09<<SHIFT, 0x06<<SHIFT, 0x23<<SHIFT }, /* Value 1 */
				{ 0x00<<SHIFT, 0x00<<SHIFT, 0x2A<<SHIFT }, /* Value 2 */
				{ 0x11<<SHIFT, 0x00<<SHIFT, 0x27<<SHIFT }, /* Value 3 */
				{ 0x23<<SHIFT, 0x00<<SHIFT, 0x1D<<SHIFT }, /* Value 4 */
				{ 0x2A<<SHIFT, 0x00<<SHIFT, 0x04<<SHIFT }, /* Value 5 */
				{ 0x29<<SHIFT, 0x00<<SHIFT, 0x00<<SHIFT }, /* Value 6 */
				{ 0x1F<<SHIFT, 0x02<<SHIFT, 0x00<<SHIFT }, /* Value 7 */
				{ 0x10<<SHIFT, 0x0B<<SHIFT, 0x00<<SHIFT }, /* Value 8 */
				{ 0x00<<SHIFT, 0x11<<SHIFT, 0x00<<SHIFT }, /* Value 9 */
				{ 0x00<<SHIFT, 0x14<<SHIFT, 0x00<<SHIFT }, /* Value 10 */
				{ 0x00<<SHIFT, 0x0F<<SHIFT, 0x05<<SHIFT }, /* Value 11 */
				{ 0x06<<SHIFT, 0x0F<<SHIFT, 0x17<<SHIFT }, /* Value 12 */
				{ 0x00<<SHIFT, 0x00<<SHIFT, 0x00<<SHIFT }, /* Value 13 */
				{ 0x00<<SHIFT, 0x00<<SHIFT, 0x00<<SHIFT }, /* Value 14 */
				{ 0x00<<SHIFT, 0x00<<SHIFT, 0x00<<SHIFT }, /* Value 15 */
				{ 0x2F<<SHIFT, 0x2F<<SHIFT, 0x2F<<SHIFT }, /* Value 16 */
				{ 0x00<<SHIFT, 0x1C<<SHIFT, 0x3B<<SHIFT }, /* Value 17 */
				{ 0x08<<SHIFT, 0x0E<<SHIFT, 0x3B<<SHIFT }, /* Value 18 */
				{ 0x20<<SHIFT, 0x00<<SHIFT, 0x3C<<SHIFT }, /* Value 19 */
				{ 0x2F<<SHIFT, 0x00<<SHIFT, 0x2F<<SHIFT }, /* Value 20 */
				{ 0x39<<SHIFT, 0x00<<SHIFT, 0x16<<SHIFT }, /* Value 21 */
				{ 0x36<<SHIFT, 0x0A<<SHIFT, 0x00<<SHIFT }, /* Value 22 */
				{ 0x32<<SHIFT, 0x13<<SHIFT, 0x03<<SHIFT }, /* Value 23 */
				{ 0x22<<SHIFT, 0x1C<<SHIFT, 0x00<<SHIFT }, /* Value 24 */
				{ 0x00<<SHIFT, 0x25<<SHIFT, 0x00<<SHIFT }, /* Value 25 */
				{ 0x00<<SHIFT, 0x2A<<SHIFT, 0x00<<SHIFT }, /* Value 26 */
				{ 0x00<<SHIFT, 0x24<<SHIFT, 0x0E<<SHIFT }, /* Value 27 */
				{ 0x00<<SHIFT, 0x20<<SHIFT, 0x22<<SHIFT }, /* Value 28 */
				{ 0x00<<SHIFT, 0x00<<SHIFT, 0x00<<SHIFT }, /* Value 29 */
				{ 0x00<<SHIFT, 0x00<<SHIFT, 0x00<<SHIFT }, /* Value 30 */
				{ 0x00<<SHIFT, 0x00<<SHIFT, 0x00<<SHIFT }, /* Value 31 */
				{ 0x3F<<SHIFT, 0x3F<<SHIFT, 0x3F<<SHIFT }, /* Value 32 */
				{ 0x0F<<SHIFT, 0x2F<<SHIFT, 0x3F<<SHIFT }, /* Value 33 */
				{ 0x17<<SHIFT, 0x25<<SHIFT, 0x3F<<SHIFT }, /* Value 34 */
				{ 0x10<<SHIFT, 0x22<<SHIFT, 0x3F<<SHIFT }, /* Value 35 */
				{ 0x3D<<SHIFT, 0x1E<<SHIFT, 0x3F<<SHIFT }, /* Value 36 */
				{ 0x3F<<SHIFT, 0x1D<<SHIFT, 0x2D<<SHIFT }, /* Value 37 */
				{ 0x3F<<SHIFT, 0x1D<<SHIFT, 0x18<<SHIFT }, /* Value 38 */
				{ 0x3F<<SHIFT, 0x26<<SHIFT, 0x0E<<SHIFT }, /* Value 39 */
				{ 0x3C<<SHIFT, 0x2F<<SHIFT, 0x0F<<SHIFT }, /* Value 40 */
				{ 0x20<<SHIFT, 0x34<<SHIFT, 0x04<<SHIFT }, /* Value 41 */
				{ 0x13<<SHIFT, 0x37<<SHIFT, 0x12<<SHIFT }, /* Value 42 */
				{ 0x16<<SHIFT, 0x3E<<SHIFT, 0x26<<SHIFT }, /* Value 43 */
				{ 0x00<<SHIFT, 0x3A<<SHIFT, 0x36<<SHIFT }, /* Value 44 */
				{ 0x1E<<SHIFT, 0x1E<<SHIFT, 0x1E<<SHIFT }, /* Value 45 */
				{ 0x00<<SHIFT, 0x00<<SHIFT, 0x00<<SHIFT }, /* Value 46 */
				{ 0x00<<SHIFT, 0x00<<SHIFT, 0x00<<SHIFT }, /* Value 47 */
				{ 0x3F<<SHIFT, 0x3F<<SHIFT, 0x3F<<SHIFT }, /* Value 48 */
				{ 0x2A<<SHIFT, 0x39<<SHIFT, 0x3F<<SHIFT }, /* Value 49 */
				{ 0x31<<SHIFT, 0x35<<SHIFT, 0x3F<<SHIFT }, /* Value 50 */
				{ 0x35<<SHIFT, 0x32<<SHIFT, 0x3F<<SHIFT }, /* Value 51 */
				{ 0x3F<<SHIFT, 0x31<<SHIFT, 0x3F<<SHIFT }, /* Value 52 */
				{ 0x3F<<SHIFT, 0x31<<SHIFT, 0x36<<SHIFT }, /* Value 53 */
				{ 0x3F<<SHIFT, 0x2F<<SHIFT, 0x2C<<SHIFT }, /* Value 54 */
				{ 0x3F<<SHIFT, 0x36<<SHIFT, 0x2A<<SHIFT }, /* Value 55 */
				{ 0x3F<<SHIFT, 0x39<<SHIFT, 0x28<<SHIFT }, /* Value 56 */
				{ 0x38<<SHIFT, 0x3F<<SHIFT, 0x28<<SHIFT }, /* Value 57 */
				{ 0x2A<<SHIFT, 0x3C<<SHIFT, 0x2F<<SHIFT }, /* Value 58 */
				{ 0x2C<<SHIFT, 0x3F<<SHIFT, 0x33<<SHIFT }, /* Value 59 */
				{ 0x27<<SHIFT, 0x3F<<SHIFT, 0x3C<<SHIFT }, /* Value 60 */
				{ 0x31<<SHIFT, 0x31<<SHIFT, 0x31<<SHIFT }, /* Value 61 */
				{ 0x00<<SHIFT, 0x00<<SHIFT, 0x00<<SHIFT }, /* Value 62 */
				{ 0x00<<SHIFT, 0x00<<SHIFT, 0x00<<SHIFT }, /* Value 63 */
			};
		
		} //class palettes
	
	} //partial class NES

} //namespace
