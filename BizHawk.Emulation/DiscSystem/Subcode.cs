using System;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;

//a decent little subcode reference
//http://www.jbum.com/cdg_revealed.html

namespace BizHawk.DiscSystem
{
	//this has been checked against mednafen's and seems to match
	//there are a few dozen different ways to do CRC16-CCITT
	//this table is backwards or something. at any rate its tailored to the needs of the Q subchannel
	internal static class CRC16_CCITT
	{
		private static ushort[] table = new ushort[256];

		static CRC16_CCITT()
		{
			ushort value;
			ushort temp;
			for (ushort i = 0; i < 256; ++i)
			{
				value = 0;
				temp = (ushort)(i << 8);
				for (byte j = 0; j < 8; ++j)
				{
					if (((value ^ temp) & 0x8000) != 0)
						value = (ushort)((value << 1) ^ 0x1021);
					else
						value <<= 1;
					temp <<= 1;
				}
				table[i] = value;
			}
		}

		public static ushort Calculate(byte[] data, int offset, int length)
		{
			ushort Result = 0;
			for(int i=0;i<length;i++)
			{
				byte b = data[offset + i];
				int index = (b ^ ((Result >> 8) & 0xFF));
				Result = (ushort)((Result << 8) ^ table[index]);
			}
			return Result;
		}

	}


	public class SubcodeDataDecoder
	{
		public static void Unpack_Q(byte[] output, int out_ofs, byte[] input, int in_ofs)
		{
			for (int i = 0; i < 12; i++)
				output[out_ofs + i] = 0;
			for (int i = 0; i < 96; i++)
			{
				int bytenum = i >> 3;
				int bitnum = i & 7;
				bitnum = 7 - bitnum;
				int bitval = (byte)((input[in_ofs + i] >> 6) & 1);
				bitval <<= bitnum;
				output[out_ofs + bytenum] |= (byte)bitval;
			}
		}
	}



}
