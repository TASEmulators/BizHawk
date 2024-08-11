using BizHawk.Common;
using BizHawk.Common.BufferExtensions;

namespace BizHawk.Emulation.DiscSystem
{
	public class DiscHasher
	{
		public DiscHasher(Disc disc)
		{
			this.disc = disc;
		}

		private readonly Disc disc;

		/// <summary>
		/// calculates the hash for quick PSX Disc identification
		/// </summary>
		public string Calculate_PSX_BizIDHash()
		{
			//notes about the hash:
			//"Arc the Lad II (J) 1.0 and 1.1 conflict up to 25 sectors (so use 26)
			//Tekken 3 (Europe) (Alt) and Tekken 3 (Europe) conflict in track 2 and 3 unfortunately, not sure what to do about this yet
			//the TOC isn't needed!
			//but it will help detect dumps with mangled TOCs which are all too common

			CRC32 crc = new();
			var buffer2352 = new byte[2352];

			var dsr = new DiscSectorReader(disc)
			{
				Policy = { DeterministicClearBuffer = false } // live dangerously
			};

			//hash the TOC
			static void AddAsBytesTo(CRC32 crc32, int i)
				=> crc32.Add(BitConverter.GetBytes(i));

			AddAsBytesTo(crc, (int)disc.TOC.SessionFormat);
			AddAsBytesTo(crc, disc.TOC.FirstRecordedTrackNumber);
			AddAsBytesTo(crc, disc.TOC.LastRecordedTrackNumber);
			for (var i = 1; i <= 100; i++)
			{
				//if (disc.TOC.TOCItems[i].Exists) Console.WriteLine("{0:X8} {1:X2} {2:X2} {3:X8}", crc.Current, (int)disc.TOC.TOCItems[i].Control, disc.TOC.TOCItems[i].Exists ? 1 : 0, disc.TOC.TOCItems[i].LBATimestamp.Sector); //a little debugging
				AddAsBytesTo(crc, (int)disc.TOC.TOCItems[i].Control);
				AddAsBytesTo(crc, disc.TOC.TOCItems[i].Exists ? 1 : 0);
				AddAsBytesTo(crc, disc.TOC.TOCItems[i].LBA);
			}

			//hash first 26 sectors
			for (var i = 0; i < 26; i++)
			{
				dsr.ReadLBA_2352(i, buffer2352, 0);
				crc.Add(buffer2352);
			}

			return CRC32Checksum.BytesAsDigest(crc.Result).BytesToHexString();
		}

		/// <summary>
		/// calculates the complete disc hash for matching to a redump
		/// </summary>
		public uint Calculate_PSX_RedumpHash()
		{
			CRC32 crc = new();
			var buffer2352 = new byte[2352];

			var dsr = new DiscSectorReader(disc)
			{
				Policy = { DeterministicClearBuffer = false } // live dangerously
			};


			//read all sectors for redump hash
			for (var i = 0; i < disc.Session1.LeadoutLBA; i++)
			{
				dsr.ReadLBA_2352(i, buffer2352, 0);
				crc.Add(buffer2352);
			}

			return crc.Result;
		}

		// gets an identifying hash. hashes the first 512 sectors of
		// the first data track on the disc.
		//TODO - this is a very platform-specific thing. hashing the TOC may be faster and be just as effective. so, rename it appropriately
		public string OldHash()
		{
			var buffer = new byte[512 * 2352];
			var dsr = new DiscSectorReader(disc);
			foreach (var track in disc.Session1.Tracks)
			{
				if (track.IsAudio)
					continue;

				var lba_len = Math.Min(track.NextTrack.LBA, 512);
				for (var s = 0; s < 512 && s < lba_len; s++)
					dsr.ReadLBA_2352(track.LBA + s, buffer, s * 2352);

				return MD5Checksum.ComputeDigestHex(buffer.AsSpan(start: 0, length: lba_len * 2352));
			}
			return "no data track found";
		}
	}
}