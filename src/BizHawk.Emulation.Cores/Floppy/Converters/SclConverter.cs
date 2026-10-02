namespace BizHawk.Emulation.Cores.Floppy
{
	/// <summary>
	/// Loads a TR-DOS .SCL disk image into the shared flux model. SCL is a packed catalogue-plus-data form
	/// (not a sector image): an 8-byte "SINCLAIR" signature, a 1-byte file count N, N 14-byte catalogue
	/// entries (filename, type, two params, sector count - i.e. a TR-DOS directory entry minus its start
	/// track/sector), the files' data concatenated in catalogue order, and a 4-byte checksum. We unpack it
	/// into a TR-DOS layout - directory on track 0, files laid out sequentially from track 1 - assigning each
	/// file its start track/sector and building the disk-info sector, then reuse the .TRD loader to produce
	/// the flux disk (zero-padding the unused tail up to the standard 80-cylinder double-sided geometry).
	/// </summary>
	public static class SclConverter
	{
		private const int SigLen = 8;              // "SINCLAIR"
		private const int SclEntry = 14;           // catalogue entry size in the SCL
		private const int TrdEntry = 16;           // TR-DOS directory entry size (adds start sector + track)
		private const int SectorSize = 256;
		private const int SectorsPerTrack = 16;
		private const int TrackZeroSectors = SectorsPerTrack; // track 0 = directory (8) + info (1) + spare
		private const int InfoOffset = 8 * SectorSize;        // disk-info sector = track 0, sector 9

		public static bool IsScl(byte[] d)
		{
			if (d == null || d.Length < SigLen + 1) return false;
			return d[0] == 'S' && d[1] == 'I' && d[2] == 'N' && d[3] == 'C'
				&& d[4] == 'L' && d[5] == 'A' && d[6] == 'I' && d[7] == 'R';
		}

		public static FluxDisk ToFluxDisk(byte[] d) => TrdConverter.ToFluxDisk(ToTrd(d));

		/// <summary>
		/// Expand an .SCL into a (trimmed) TR-DOS .TRD sector image.
		/// </summary>
		public static byte[] ToTrd(byte[] d)
		{
			if (!IsScl(d)) throw new System.ArgumentException("not an SCL file (no SINCLAIR signature)", nameof(d));

			int n = d[SigLen];
			int catOffset = SigLen + 1;
			int dataOffset = catOffset + n * SclEntry;

			// total data sectors (byte 13 of each entry) sizes the image
			int totalDataSectors = 0;
			for (int i = 0; i < n; i++)
			{
				int e = catOffset + i * SclEntry;
				if (e + SclEntry > d.Length) break;
				totalDataSectors += d[e + 13];
			}

			int trdSectors = TrackZeroSectors + totalDataSectors;
			var trd = new byte[trdSectors * SectorSize];

			int sectorPtr = SectorsPerTrack; // first data logical sector = track 1, sector 0
			int src = dataOffset;
			for (int i = 0; i < n; i++)
			{
				int e = catOffset + i * SclEntry;
				if (e + SclEntry > d.Length) break;
				int dst = i * TrdEntry;

				// copy the 14 SCL bytes (filename..sector count), then assign the start sector/track
				System.Array.Copy(d, e, trd, dst, SclEntry);
				trd[dst + 14] = (byte)(sectorPtr % SectorsPerTrack);
				trd[dst + 15] = (byte)(sectorPtr / SectorsPerTrack);

				int secs = d[e + 13];
				int bytes = secs * SectorSize;
				int avail = System.Math.Max(0, System.Math.Min(bytes, d.Length - src));
				if (avail > 0) System.Array.Copy(d, src, trd, sectorPtr * SectorSize, avail);
				src += bytes;
				sectorPtr += secs;
			}

			// disk-info sector (track 0, sector 9)
			trd[InfoOffset + 0xE1] = (byte)(sectorPtr % SectorsPerTrack); // first free sector
			trd[InfoOffset + 0xE2] = (byte)(sectorPtr / SectorsPerTrack); // first free track
			trd[InfoOffset + 0xE3] = 0x16;                                // disk type: 80 track, double sided
			trd[InfoOffset + 0xE4] = (byte)n;                             // number of files
			int freeSectors = 80 * 2 * SectorsPerTrack - sectorPtr;       // usable sectors still free
			trd[InfoOffset + 0xE5] = (byte)(freeSectors & 0xFF);
			trd[InfoOffset + 0xE6] = (byte)((freeSectors >> 8) & 0xFF);
			trd[InfoOffset + 0xE7] = 0x10;                                // TR-DOS marker
			for (int j = 0; j < 8; j++) trd[InfoOffset + 0xF5 + j] = 0x20; // blank disk label

			return trd;
		}
	}
}
