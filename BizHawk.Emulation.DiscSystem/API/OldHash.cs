using System;

using BizHawk.Common.BufferExtensions;

namespace BizHawk.Emulation.DiscSystem
{
	public class OldHasher
	{
		public OldHasher(Disc disc)
		{
			this.disc = disc;
		}

		Disc disc;

		// gets an identifying hash. hashes the first 512 sectors of 
		// the first data track on the disc.
		//TODO - this is a very platform-specific thing. hashing the TOC may be faster and be just as effective. so, rename it appropriately
		public string GetHash()
		{
			byte[] buffer = new byte[512 * 2352];
			DiscSectorReader dsr = new DiscSectorReader(disc);
			foreach (var track in disc.Structure.Sessions[0].Tracks)
			{
				if (track.TrackType == DiscStructure.ETrackType.Audio)
					continue;

				int lba_len = Math.Min(track.LengthInSectors, 512);
				for (int s = 0; s < 512 && s < track.LengthInSectors; s++)
					dsr.ReadLBA_2352(track.Indexes[1].LBA + s, buffer, s * 2352);

				return buffer.HashMD5(0, lba_len * 2352);
			}
			return "no data track found";
		}
	}
}