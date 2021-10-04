#nullable disable

using System;

namespace BizHawk.Common
{
	/// <summary>
	/// A stateful special CRC32 calculator
	/// This may be absolutely standard and not special at all. I don't know, there were some differences between it and other CRC code I found in bizhawk
	/// </summary>
	public class SpecialCRC32
	{
		private static readonly uint[] CRC32Table;

		static SpecialCRC32()
		{
			CRC32Table = new uint[256];
			for (uint i = 0; i < 256; ++i)
			{
				uint crc = i;
				for (int j = 8; j > 0; --j)
				{
					if ((crc & 1) == 1)
						crc = ((crc >> 1) ^ 0xEDB88320);
					else
						crc >>= 1;
				}
				CRC32Table[i] = crc;
			}
		}

		private uint current = 0xFFFFFFFF;

		public void Add(ReadOnlySpan<byte> data)
		{
			foreach (var b in data) current = CRC32Table[(current ^ b) & 0xFF] ^ (current >> 8);
		}

		/// <summary>
		/// The negated output (the typical result of the CRC calculation)
		/// </summary>
		public uint Result => current ^ 0xFFFFFFFF;

		/// <summary>
		/// The raw non-negated output
		/// </summary>
		public uint Current
		{
			get => current;
			set => current = value;
		}

		private uint gf2_matrix_times(uint[] mat, uint vec)
		{
			int matIdx = 0;
			uint sum = 0;
			while (vec != 0)
			{
				if ((vec & 1) != 0)
					sum ^= mat[matIdx];
				vec >>= 1;
				matIdx++;
			}
			return sum;
		}

		private void gf2_matrix_square(uint[] square, uint[] mat)
		{
			int n;
			for (n = 0; n < 32; n++)
				square[n] = gf2_matrix_times(mat, mat[n]);
		}

		/// <summary>
		/// Incorporates a pre-calculated CRC with the given length by combining crcs
		/// It's a bit flaky, so be careful, but it works
		/// </summary>
		public void Incorporate(uint crc, int len)
		{
			current = crc32_combine(current, crc, len);
		}

		//tables used by crc32_combine
		private uint[] even, odd;

		//algorithm from zlib's crc32_combine. read http://www.leapsecond.com/tools/crcomb.c for more
		private uint crc32_combine(uint crc1, uint crc2, int len2)
		{
			even ??= new uint[32]; // even-power-of-two zeros operator
			odd ??= new uint[32]; // odd-power-of-two zeros operator

			// degenerate case
			if (len2 == 0)
				return crc1;

			// put operator for one zero bit in odd
			odd[0] = 0xedb88320;           //CRC-32 polynomial
			uint row = 1;
			for (int n = 1; n < 32; n++)
			{
				odd[n] = row;
				row <<= 1;
			}

			//put operator for two zero bits in even
			gf2_matrix_square(even, odd);

			//put operator for four zero bits in odd
			gf2_matrix_square(odd, even);

			//apply len2 zeros to crc1 (first square will put the operator for one zero byte, eight zero bits, in even)
			do
			{
				//apply zeros operator for this bit of len2
				gf2_matrix_square(even, odd);
				if ((len2 & 1) != 0)
					crc1 = gf2_matrix_times(even, crc1);
				len2 >>= 1;

				//if no more bits set, then done
				if (len2 == 0)
					break;

				//another iteration of the loop with odd and even swapped
				gf2_matrix_square(odd, even);
				if ((len2 & 1) != 0)
					crc1 = gf2_matrix_times(odd, crc1);
				len2 >>= 1;

				//if no more bits set, then done
			} while (len2 != 0);

			//return combined crc
			crc1 ^= crc2;
			return crc1;
		}
	}
}
