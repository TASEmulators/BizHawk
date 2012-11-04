//Currently, this is stolen somewhat from blargg's code. probably LGPL. I think? originally it came from mednafen, and then I think mednafen may have changed to use another.
//sorry for the confusion! it'd be nice to have some nice MIT licensed CD algorithms codes.. at least, we can keep these bullshit licensed ones here to a/b test when rewriting.

namespace BizHawk.DiscSystem
{
	static class ECM
	{
		static byte[] ecc_f_lut = new byte[256];
		static byte[] ecc_b_lut = new byte[256];
		static uint[] edc_lut = new uint[256];

		static ECM()
		{
			uint i, j, edc;
			for(i = 0; i < 256; i++)
			{
				j = (uint)((i << 1) ^ (((i & 0x80) != 0) ? 0x11D : 0));
				ecc_f_lut[i] = (byte)j;
				ecc_b_lut[i ^ j] = (byte)i;
				edc = i;
				for (j = 0; j < 8; j++) 
					edc = (edc >> 1) ^ (((edc & 1) != 0) ? 0xD8018001 : 0);
				edc_lut[i] = edc;
			}
		}

		static uint edc_partial_computeblock(uint edc, byte[] src, int count)
		{
			int i = 0;
			while (count-- != 0) edc = (edc >> 8) ^ edc_lut[(edc ^ (src[i++])) & 0xFF];
			return edc;
		}

		public static void edc_computeblock(byte[] src, int count, byte[] dest, int dest_offset)
		{
			uint edc = edc_partial_computeblock(0, src, count);
			dest[dest_offset + 0] = (byte)((edc >> 0) & 0xFF);
			dest[dest_offset + 1] = (byte)((edc >> 8) & 0xFF);
			dest[dest_offset + 2] = (byte)((edc >> 16) & 0xFF);
			dest[dest_offset + 3] = (byte)((edc >> 24) & 0xFF);
		}

		static void ecc_computeblock(byte[] src, int src_offset, uint major_count, uint minor_count,uint major_mult, uint minor_inc, byte[] dest, int dest_offset)
		{
			uint size = major_count * minor_count;
			uint major, minor;
			for (major = 0; major < major_count; major++)
			{
				uint index = (major >> 1) * major_mult + (major & 1);
				byte ecc_a = 0;
				byte ecc_b = 0;
				for (minor = 0; minor < minor_count; minor++)
				{
					byte temp = src[src_offset+index];
					index += minor_inc;
					if (index >= size) index -= size;
					ecc_a ^= temp;
					ecc_b ^= temp;
					ecc_a = ecc_f_lut[ecc_a];
					//System.Console.WriteLine("{0} {1}", ecc_a, ecc_b);
				}
				ecc_a = ecc_b_lut[ecc_f_lut[ecc_a] ^ ecc_b];
				dest[dest_offset + major] = ecc_a;
				dest[dest_offset + major + major_count] = (byte)(ecc_a ^ ecc_b);
			}
		}

		public unsafe static void ecc_generate(byte[] sector, int sector_offset, bool zeroaddress, byte[] dest, int dest_offset)
		{
		  byte address0=0,address1=0,address2=0,address3=0;
			//byte i;
		  /* Save the address and zero it out */
		  if(zeroaddress)
		  {
			  address0 = sector[sector_offset + 12 + 0]; sector[sector_offset + 12 + 0] = 0;
			  address1 = sector[sector_offset + 12 + 1]; sector[sector_offset + 12 + 1] = 0;
			  address2 = sector[sector_offset + 12 + 2]; sector[sector_offset + 12 + 2] = 0;
			  address3 = sector[sector_offset + 12 + 3]; sector[sector_offset + 12 + 3] = 0;
		  }
		  /* Compute ECC P code */
		  ecc_computeblock(sector, sector_offset + 0xC, 86, 24, 2, 86, dest, dest_offset);
		  /* Compute ECC Q code */
		  ecc_computeblock(sector, sector_offset + 0xC, 52, 43, 86, 88, dest, dest_offset+172);
		  /* Restore the address */
		  if (zeroaddress)
		  {
			  sector[sector_offset + 12 + 0] = address0;
			  sector[sector_offset + 12 + 3] = address1;
			  sector[sector_offset + 12 + 2] = address2;
			  sector[sector_offset + 12 + 1] = address3;
		  }
		}
	}

}