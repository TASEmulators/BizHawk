using System;

using BizHawk.Common.BufferExtensions;

namespace BizHawk.Emulation.DiscSystem
{
	public class DiscHasher
	{
		public DiscHasher(Disc disc)
		{
			this.disc = disc;
		}

		Disc disc;

		/// <summary>
		/// calculates the hash for quick PSX Disc identification
		/// </summary>
		public uint Calculate_PSX_BizIDHash()
		{
			//notes about the hash:
			//"Arc the Lad II (J) 1.0 and 1.1 conflict up to 25 sectors (so use 26)
			//Tekken 3 (Europe) (Alt) and Tekken 3 (Europe) conflict in track 2 and 3 unfortunately, not sure what to do about this yet
			//the TOC isn't needed!
			//but it will help detect dumps with mangled TOCs which are all too common
			//
			//a possibly special CRC32 is used to help us match redump's DB elsewhere

			SpecialCRC32 crc = new SpecialCRC32();
			byte[] buffer2352 = new byte[2352];

			var dsr = new DiscSectorReader(disc);
			dsr.Policy.DeterministicClearBuffer = false; //live dangerously

			//hash the TOC
			crc.Add((int)disc.TOC.Session1Format);
			crc.Add(disc.TOC.FirstRecordedTrackNumber);
			crc.Add(disc.TOC.LastRecordedTrackNumber);
			for (int i = 1; i <= 100; i++)
			{
				//if (disc.TOC.TOCItems[i].Exists) Console.WriteLine("{0:X8} {1:X2} {2:X2} {3:X8}", crc.Current, (int)disc.TOC.TOCItems[i].Control, disc.TOC.TOCItems[i].Exists ? 1 : 0, disc.TOC.TOCItems[i].LBATimestamp.Sector); //a little debugging
				crc.Add((int)disc.TOC.TOCItems[i].Control);
				crc.Add(disc.TOC.TOCItems[i].Exists ? 1 : 0);
				crc.Add((int)disc.TOC.TOCItems[i].LBA);
			}

			//hash first 26 sectors
			for (int i = 0; i < 26; i++)
			{
				dsr.ReadLBA_2352(i, buffer2352, 0);
				crc.Add(buffer2352, 0, 2352);
			}

			return crc.Result;
		}

		/// <summary>
		/// calculates the complete disc hash for matching to a redump
		/// </summary>
		public uint Calculate_PSX_RedumpHash()
		{
			//a special CRC32 is used to help us match redump's DB
			SpecialCRC32 crc = new SpecialCRC32();
			byte[] buffer2352 = new byte[2352];

			var dsr = new DiscSectorReader(disc);
			dsr.Policy.DeterministicClearBuffer = false; //live dangerously

			//read all sectors for redump hash
			for (int i = 0; i < disc.Session1.LeadoutLBA; i++)
			{
				dsr.ReadLBA_2352(i, buffer2352, 0);
				crc.Add(buffer2352, 0, 2352);
			}

			return crc.Result;
		}

		// gets an identifying hash. hashes the first 512 sectors of 
		// the first data track on the disc.
		//TODO - this is a very platform-specific thing. hashing the TOC may be faster and be just as effective. so, rename it appropriately
		public string OldHash()
		{
			byte[] buffer = new byte[512 * 2352];
			DiscSectorReader dsr = new DiscSectorReader(disc);
			foreach (var track in disc.Session1.Tracks)
			{
				if (track.IsAudio)
					continue;

				int lba_len = Math.Min(track.NextTrack.LBA, 512);
				for (int s = 0; s < 512 && s < lba_len; s++)
					dsr.ReadLBA_2352(track.LBA + s, buffer, s * 2352);

				return buffer.HashMD5(0, lba_len * 2352);
			}
			return "no data track found";
		}

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

			uint current = 0xFFFFFFFF;
			public unsafe void Add(byte[] data, int offset, int size)
			{
				if (offset + size > data.Length)
					throw new ArgumentOutOfRangeException();
				if (offset < 0)
					throw new ArgumentOutOfRangeException();
				fixed (byte* pData = data)
					for (int i = 0; i < size; i++)
					{
						byte b = pData[offset + i];
						current = CRC32Table[(current ^ b) & 0xFF] ^ (current >> 8);
					}
			}

			byte[] smallbuf = new byte[8];
			public void Add(int data)
			{
				smallbuf[0] = (byte)((data) & 0xFF);
				smallbuf[1] = (byte)((data >> 8) & 0xFF);
				smallbuf[2] = (byte)((data >> 16) & 0xFF);
				smallbuf[3] = (byte)((data >> 24) & 0xFF);
				Add(smallbuf, 0, 4);
			}

			/// <summary>
			/// The negated output (the typical result of the CRC calculation)
			/// </summary>
			public uint Result { get { return current ^ 0xFFFFFFFF; } }

			/// <summary>
			/// The raw non-negated output
			/// </summary>
			public uint Current { get { return current; } set { current = value; } }

			uint gf2_matrix_times(uint[] mat, uint vec)
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

			void gf2_matrix_square(uint[] square, uint[] mat)
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
			uint[] even, odd;

			//algorithm from zlib's crc32_combine. read http://www.leapsecond.com/tools/crcomb.c for more
			uint crc32_combine(uint crc1, uint crc2, int len2)
			{
				if (even == null) even = new uint[32];    // even-power-of-two zeros operator
				if (odd == null) odd = new uint[32];    // odd-power-of-two zeros operator

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
}