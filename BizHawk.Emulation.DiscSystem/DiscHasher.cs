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

		class SpecialCRC32
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
				if(offset<0)
					throw new ArgumentOutOfRangeException();
				fixed(byte* pData = data)
					for (int i = 0; i < size; i++)
					{
						byte b = pData[offset + i];
						current = CRC32Table[(current ^ b) & 0xFF] ^ (current>>8);
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

			public uint Result { get { return current ^ 0xFFFFFFFF; } }
		}

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
			//a special CRC32 is used to help us match redump's DB elsewhere

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
				crc.Add((int)disc.TOC.TOCItems[i].Control);
				crc.Add(disc.TOC.TOCItems[i].Exists ? 1 : 0);
				crc.Add((int)disc.TOC.TOCItems[i].LBATimestamp.Sector);
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
	}
}