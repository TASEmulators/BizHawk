#nullable enable

using System.Collections.Generic;
using System.Text;
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

		public string CalculateBizHash(DiscType discType)
		{
			return discType switch
			{
				DiscType.SonyPSX => Calculate_PSX_BizIDHash(),
				DiscType.JaguarCD => CalculateRAJaguarHash() ?? "",
				_ => OldHash(),
			};
		}

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
			// don't hash generated lead-in and lead-out tracks
			for (int i = 1; i <= disc.Session1.InformationTrackCount; i++)
			{
				var track = disc.Session1.Tracks[i];

				if (track.IsAudio)
					continue;

				var lba_len = Math.Min(track.NextTrack.LBA - track.LBA, 512);
				for (var s = 0; s < 512 && s < lba_len; s++)
					dsr.ReadLBA_2352(track.LBA + s, buffer, s * 2352);

				return MD5Checksum.ComputeDigestHex(buffer.AsSpan(start: 0, length: lba_len * 2352));
			}
			return "no data track found";
		}

		/// <summary>
		/// Calculate Jaguar CD hash according to RetroAchievements logic
		/// </summary>
		public string? CalculateRAJaguarHash()
		{
			if (disc.Sessions.Count <= 2)
			{
				return null;
			}

			var dsr = new DiscSectorReader(disc)
			{
				Policy = { DeterministicClearBuffer = false } // let's make this a little faster
			};

			static string? HashJaguar(DiscTrack bootTrack, DiscSectorReader dsr, bool commonHomebrewHash)
			{
				const string _jaguarHeader = "ATARI APPROVED DATA HEADER ATRI";
				const string _jaguarBSHeader = "TARA IPARPVODED TA AEHDAREA RT";
				var buffer = new List<byte>();
				var buf2352 = new byte[2352];

				// find the boot track header
				// see https://github.com/TASEmulators/BizHawk/blob/f29113287e88c6a644dbff30f92a9833307aad20/waterbox/virtualjaguar/src/cdhle.cpp#L109-L145
				var startLba = bootTrack.LBA;
				var numLbas = bootTrack.NextTrack.LBA - bootTrack.LBA;
				int bootLen = 0, bootLba = 0, bootOff = 0;
				bool byteswapped = false, foundHeader = false;
				var bootLenOffset = (commonHomebrewHash ? 0x40 : 0) + 32 + 4;
				for (var i = 0; i < numLbas; i++)
				{
					dsr.ReadLBA_2352(startLba + i, buf2352, 0);

					for (var j = 0; j < 2352 - bootLenOffset - 4; j++)
					{
						if (buf2352[j] == _jaguarHeader[0])
						{
							if (_jaguarHeader == Encoding.ASCII.GetString(buf2352, j, 32 - 1))
							{
								bootLen = (buf2352[j + bootLenOffset + 0] << 24) | (buf2352[j + bootLenOffset + 1] << 16) |
									(buf2352[j + bootLenOffset + 2] << 8) | buf2352[j + bootLenOffset + 3];
								bootLba = startLba + i;
								bootOff = j + bootLenOffset + 4;
								// byteswapped = false;
								foundHeader = true;
								break;
							}
						}
						else if (buf2352[j] == _jaguarBSHeader[0])
						{
							if (_jaguarBSHeader == Encoding.ASCII.GetString(buf2352, j, 32 - 2))
							{
								bootLen = (buf2352[j + bootLenOffset + 1] << 24) | (buf2352[j + bootLenOffset + 0] << 16) |
									(buf2352[j + bootLenOffset + 3] << 8) | buf2352[j + bootLenOffset + 2];
								bootLba = startLba + i;
								bootOff = j + bootLenOffset + 4;
								byteswapped = true;
								foundHeader = true;
								break;
							}
						}
					}

					if (foundHeader)
					{
						break;
					}
				}

				if (!foundHeader)
				{
					return null;
				}

				dsr.ReadLBA_2352(bootLba++, buf2352, 0);

				if (byteswapped)
				{
					EndiannessUtils.MutatingByteSwap16(buf2352.AsSpan());
				}

				buffer.AddRange(new ArraySegment<byte>(buf2352, bootOff, Math.Min(2352 - bootOff, bootLen)));
				bootLen -= 2352 - bootOff;

				while (bootLen > 0)
				{
					dsr.ReadLBA_2352(bootLba++, buf2352, 0);

					if (byteswapped)
					{
						EndiannessUtils.MutatingByteSwap16(buf2352.AsSpan());
					}

					buffer.AddRange(new ArraySegment<byte>(buf2352, 0, Math.Min(2352, bootLen)));
					bootLen -= 2352;
				}

				return MD5Checksum.ComputeDigestHex(buffer.ToArray());
			}

			var jaguarHash = HashJaguar(disc.Sessions[2].Tracks[1], dsr, false);

			if (jaguarHash is "254487B59AB21BC005338E85CBF9FD2F") // see https://github.com/RetroAchievements/rcheevos/pull/234
			{
				jaguarHash = HashJaguar(disc.Sessions[1].Tracks[2], dsr, true);
			}

			return jaguarHash;
		}
	}
}