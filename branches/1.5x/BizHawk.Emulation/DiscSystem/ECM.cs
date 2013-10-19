//Copyright (c) 2012 BizHawk team

//Permission is hereby granted, free of charge, to any person obtaining a copy of 
//this software and associated documentation files (the "Software"), to deal in
//the Software without restriction, including without limitation the rights to
//use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
//of the Software, and to permit persons to whom the Software is furnished to do
//so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all 
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

//CD-ROM ECC/EDC related algorithms

//todo - ecm sometimes sets the sector address to 0 before computing the ECC. i cant find any documentation to support this.
//seems to only take effect for cd-xa (mode 2, form 1). need to ask about this or test further on a cd-xa test disc

//regarding ECC:
//it turns out mame's cdrom.c uses pretty much this exact thing, naming the tables mul2tab->ecclow and div3tab->ecchigh
//Corlett's ECM uses our same fundamental approach as well.
//I can't figure out what winUAE is doing.

namespace BizHawk.DiscSystem
{
	static class ECM
	{
		//EDC (crc) acceleration table
		static uint[] edc_table = new uint[256];

		//math acceleration tables over GF(2^8) with yellowbook specified primitive polynomial 0x11D
		static byte[] mul2tab = new byte[256];
		static byte[] div3tab = new byte[256];

		static ECM()
		{
			Prep_EDC();
			Prep_ECC();
		}

		/// <summary>
		/// calculate EDC crc tables
		/// </summary>
		static void Prep_EDC()
		{
			//14.3 of yellowbook specifies EDC crc as P(x) = (x^16 + x^15 + x^2 + 1) . (x^16 + x^2 + x + 1) 
			//confirmation at http://cdmagediscussion.yuku.com/topic/742/EDC-calculation
			//int Pa = 0x18005;
			//int Pb = 0x10007;
			//long Px = 0;
			//for (int i = 0; i <= 16; i++)
			//  for (int j = 0; j <= 16; j++)
			//  {
			//    //multiply Pa[i] * Pb[j]
			//    int bit = (Pa >> i) & (Pb >> j) & 1;
			//    //xor into result, achieving modulo-2 thereby
			//    Px ^= (long)bit << (i + j);
			//  }
			//uint edc_poly = (uint)Px;
			uint edc_poly = (uint)0x8001801B;

			//generate the CRC table 
			uint reverse_edc_poly = BITREV.reverse_32(edc_poly);
			for (uint i = 0; i < 256; ++i)
			{
				uint crc = i;
				for (int j = 8; j > 0; --j)
				{
					if ((crc & 1) == 1)
						crc = ((crc >> 1) ^ reverse_edc_poly);
					else
						crc >>= 1;
				}
				edc_table[i] = crc;
			}
		}

		/// <summary>
		/// calculate math lookup tables for ECC calculations. 
		/// </summary>
		static void Prep_ECC()
		{
			//create a table implementing f(i) = i*2 
			for (int i = 0; i < 256; i++)
			{
				int n = i * 2;
				int b = n & 0xFF;
				if (n > 0xFF) b ^= 0x1D; //primitive polynomial x^8 + x^4 + x^3 + x^2 + 1 -> 0x11D
				mul2tab[i] = (byte)b;
			}

			//(here is a more straightforward way of doing it, just to check)
			//byte[] mul2tab_B = new byte[256];
			//for (int i = 1; i < 256; i++)
			//  mul2tab_B[i] = FFUtil.gf_mul((byte)i, (byte)2);

			////(make sure theyre the same)
			//for (int i = 0; i < 256; i++)
			//  System.Diagnostics.Debug.Assert(mul2tab[i] == mul2tab_B[i]);

			//create a table implementing f(i) = i/3
			for (int i = 0; i < 256; i++)
			{
				byte x1 = (byte)i;
				byte x2 = mul2tab[i];
				byte x3 = (byte)(x2 ^ x1); //2x + x = 3x
				//instead of dividing 1/3 we write the table backwards since its the inverse of multiplying by 3
				//this idea was taken from Corlett's techniques; I know not from whence they came.
				div3tab[x3] = x1;
			}

			//(here is a more straightforward way of doing it, just to check)
			//byte[] div3tab_B = new byte[256];
			//for (int i = 0; i < 256; i++)
			//  div3tab_B[i] = FFUtil.gf_div((byte)i, 3);

			////(make sure theyre the same)
			//for (int i = 0; i < 256; i++)
			//  System.Diagnostics.Debug.Assert(div3tab[i] == div3tab_B[i]);
		}

		/// <summary>
		/// Calculates ECC parity values for the specified data
		/// see annex A of yellowbook
		/// </summary>
		public static void CalcECC(byte[] data, int base_offset, int addr_offset, int addr_add, int todo, out byte p0, out byte p1)
		{
			//let's take the P parity as an example. Q parity will differ by being a (45, 43) code instead of the (25, 23) illustrated here.
			//
			//we're supposed to multiply [ [ 1 1 1 ] [a^25 a^24 a^23 ..] ] by [ [d0] [d1] [d2] ..] and get 0
			//where a is the primitive element a=2 and d0 is data[0]
			//this multiplication yields a matrix equation:
			//[ [d0 + d1 + d2]  [d0*a^25 + d1*a^24 + d2*a^23] ] = [ [0] [0] ]
			//so now we have equations:
			//(d0 + d1 + d2 ..) + p0 + p1 = 0
			//(d0*a^25 + d1*a^24 + d2*a^23 ..) + p0*a + p1 = 0
			//lets rename these series expressions for convenience to add_accum and pow_accum, respectively. so we'd have:
			//add_accum + p0 + p1 = 0
			//(pow_accum + p0) * 2 + p1 = 0
			//
			//we can get pow_accum in iterations of multiplying by 2 and adding..
			//
			//I.
			//pow_accum = d0 [add]
			//pow_accum = d0*2 [mul]
			//II.
			//pow_accum = d0*2 + d1 [add]
			//pow_accum = (d0*2 + d1)*2 [mul]
			//
			//.. and we can get add_accum in the obvious way in iterations by adding.
			//
			//now all that remains is to solve the equations:
			//(pow_accum + p0) * 2 + p1 = 0
			//(pow_accum + p0) * 2 + add_accum + p0 = 0
			//2*pow_accum + 2*p0 + add_accum + p0 = 0
			//3*p0 + p0 = -2*pow_accum - add_accum
			//3*p0 = (2*pow_accum) ^ add_accum
			//p0 = ((2*pow_accum) ^ add_accum) / 3
			//..and..
			//add_accum + p0 + p1 = 0
			//p1 = - p0 - add_accum
			//p1 = p0 ^ add_accum

			byte pow_accum = 0;
			byte add_accum = 0;
			for (int i = 0; i < todo; i++)
			{
				addr_offset %= (1118 * 2); //modulo addressing is irrelevant for P-parity calculation but comes into play for Q-parity
				byte d = data[base_offset + addr_offset];
				addr_offset += addr_add;
				add_accum ^= d;
				pow_accum ^= d;
				pow_accum = mul2tab[pow_accum];
			}

			p0 = div3tab[mul2tab[pow_accum] ^ add_accum];
			p1 = (byte)(p0 ^ add_accum);
		}

		/// <summary>
		/// handy for stashing the EDC somewhere with little endian
		/// </summary>
		public static void PokeUint(byte[] data, int offset, uint value)
		{
			data[offset + 0] = (byte)((value >> 0) & 0xFF);
			data[offset + 1] = (byte)((value >> 8) & 0xFF);
			data[offset + 2] = (byte)((value >> 16) & 0xFF);
			data[offset + 3] = (byte)((value >> 24) & 0xFF);
		}

		/// <summary>
		/// calculates EDC checksum for the range of data provided
		/// see section 14.3 of yellowbook 
		/// </summary>
		public static uint EDC_Calc(byte[] data, int offset, int length)
		{
			uint crc = 0;
			for (int i = 0; i < length; i++)
			{
				byte b = data[offset + i];
				int entry = ((int)crc ^ b) & 0xFF;
				crc = edc_table[entry] ^ (crc >> 8);
			}

			return crc;
		}



		/// <summary>
		/// returns the address from a sector. useful for saving it before zeroing it for ECC calculations
		/// </summary>
		static uint GetSectorAddress(byte[] sector, int sector_offset)
		{
			return (uint)(
				(sector[sector_offset + 12 + 0] << 0)
				| (sector[sector_offset + 12 + 1] << 8)
				| (sector[sector_offset + 12 + 2] << 16)
				| (sector[sector_offset + 12 + 3] << 24));
		}

		/// <summary>
		/// sets the address for a sector. useful for restoring it after zeroing it for ECC calculations
		/// </summary>
		static void SetSectorAddress(byte[] sector, int sector_offset, uint address)
		{
			sector[sector_offset + 12 + 0] = (byte)((address >> 0) & 0xFF);
			sector[sector_offset + 12 + 1] = (byte)((address >> 8) & 0xFF);
			sector[sector_offset + 12 + 2] = (byte)((address >> 16) & 0xFF);
			sector[sector_offset + 12 + 3] = (byte)((address >> 24) & 0xFF);
		}



		/// <summary>
		/// populates a sector with valid ECC information.
		/// it is safe to supply the same array for sector and dest.
		/// </summary>
		public static void ECC_Populate(byte[] src, int src_offset, byte[] dest, int dest_offset, bool zeroSectorAddress)
		{
			//save the old sector address, so we can restore it later. SOMETIMES ECC is supposed to be calculated without it? see TODO
			uint address = GetSectorAddress(src, src_offset);
			if (zeroSectorAddress) SetSectorAddress(src, src_offset, 0);

			//all further work takes place relative to offset 12 in the sector
			src_offset += 12;
			dest_offset += 12;

			//calculate P parity for 86 columns (twice 43 word-columns)
			byte parity0, parity1;
			for (int col = 0; col < 86; col++)
			{
				int offset = col;
				CalcECC(src, src_offset, offset, 86, 24, out parity0, out parity1);
				//store the parities in the sector; theyre read for the Q parity calculations
				dest[dest_offset + 1032 * 2 + col] = parity0;
				dest[dest_offset + 1032 * 2 + col + 43 * 2] = parity1;
			}

			//calculate Q parity for 52 diagonals (twice 26 word-diagonals)
			//modulo addressing is taken care of in CalcECC
			for (int d = 0; d < 26; d++)
			{
				for (int w = 0; w < 2; w++)
				{
					int offset = d * 86 + w;
					CalcECC(src, src_offset, offset, 88, 43, out parity0, out parity1);
					//store the parities in the sector; thats where theyve got to go anyway
					dest[dest_offset + 1118 * 2 + d * 2 + w] = parity0;
					dest[dest_offset + 1118 * 2 + d * 2 + w + 26 * 2] = parity1;
				}
			}

			//unadjust the offset back to an absolute sector address, which SetSectorAddress expects
			src_offset -= 12;
			SetSectorAddress(src, src_offset, address);
		}


		///// <summary>
		///// Finite Field math helpers. Adapted from: http://en.wikiversity.org/wiki/Reed%E2%80%93Solomon_codes_for_coders
		///// Only used by alternative implementations of ECM techniques
		///// </summary>
		//static class FFUtil
		//{
		//  public static byte gf_div(byte x, byte y)
		//  {
		//    if (y == 0)
		//      return 0; //? error ?
		//    if (x == 0)
		//      return 0;
		//    int q = gf_log[x] + 255 - gf_log[y];
		//    return gf_exp[q];
		//  }

		//  public static byte gf_mul(byte x, byte y)
		//  {
		//    if (x == 0 || y == 0)
		//      return 0;
		//    return gf_exp[gf_log[x] + gf_log[y]];
		//  }

		//  static byte[] gf_exp = new byte[512];
		//  static byte[] gf_log = new byte[256];
		//  static FFUtil()
		//  {
		//    for (int i = 0; i < 512; i++) gf_exp[i] = 1;
		//    for (int i = 0; i < 256; i++) gf_log[i] = 0;
		//    int x = 1;
		//    for (int i = 1; i < 255; i++)
		//    {
		//      x <<= 1;
		//      if ((x & 0x100) != 0)
		//        x ^= 0x11d; //yellowbook specified primitive polynomial
		//      gf_exp[i] = (byte)x;
		//      gf_log[x] = (byte)i;
		//    }
		//    for (int i = 255; i < 512; i++)
		//      gf_exp[i] = gf_exp[(byte)(i - 255)];
		//  }

		//} //static class FFUtil

	} //static class ECM

}
