using System.Collections.Generic;
using System.Text;

using BizHawk.Emulation.Cores.Floppy;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
	/// <summary>
	/// Reads the +3DOS (CP/M 2.2-style) directory off a +3 disk's flux, listing the files it contains. Useful
	/// for diagnostics / OSD - e.g. to see what to LOAD when a disk is a data disk rather than a self-booting
	/// one, or to confirm a disk decoded into a usable filesystem. Works on the shared <see cref="FluxDisk"/>
	/// (side 0), reading the +3 disk specification from logical sector 0 when present, otherwise assuming the
	/// standard 180K +3 DATA format. This reads the filesystem only; it does not interpret copy protection.
	/// </summary>
	public static class Plus3DosDirectory
	{
		public sealed class Entry
		{
			public int UserNumber;
			public string Name;      // "NAME.EXT" (or just "NAME")
			public long SizeBytes;   // sum of the records across the file's extents * 128
			public bool ReadOnly;
			public bool SystemHidden;
			public bool Archived;

			public override string ToString()
			{
				var attr = (ReadOnly ? "R" : "-") + (SystemHidden ? "S" : "-") + (Archived ? "A" : "-");
				return $"{Name,-13} {SizeBytes,7} {attr}" + (UserNumber == 0 ? "" : $" (user {UserNumber})");
			}
		}

		/// <summary>Read the directory from side 0 of the disk. Returns an empty list if no valid +3DOS
		/// directory is present (e.g. a custom-formatted / non-filesystem disk).</summary>
		public static List<Entry> Read(FluxDisk disk)
		{
			var files = new List<Entry>();
			if (disk == null) return files;

			// figure out where the directory lives: read the +3 disk spec from logical sector 0 if it looks
			// valid, otherwise fall back to the standard 180K +3 DATA format (0 reserved tracks, 64 entries).
			int reservedTracks = 0, dirEntries = 64;
			var spec = LogicalSector0(disk);
			if (spec != null && spec.Length >= 10 && spec[0] <= 3
				&& (spec[2] == 40 || spec[2] == 80) && spec[4] == 2 && spec[6] == 3)
			{
				reservedTracks = spec[5];
				int blockSize = 128 << spec[6];
				dirEntries = spec[7] * blockSize / 32;
			}

			// the directory occupies the first (dirEntries*32) bytes of the first track after the reserved ones,
			// taken from that track's sectors in logical (ascending R) order
			byte[] dir = ReadTrackBytes(disk, reservedTracks, dirEntries * 32);
			if (dir == null) return files;

			// accumulate file size across extents (CP/M splits large files into 32-byte extent records)
			var byName = new Dictionary<string, Entry>();
			for (int off = 0; off + 32 <= dir.Length; off += 32)
			{
				int user = dir[off];
				if (user > 15) continue; // 0xE5 = empty/deleted, or a non-file (label/timestamp) entry

				var sb = new StringBuilder(12);
				for (int i = 1; i <= 8; i++) { char c = (char)(dir[off + i] & 0x7F); if (c != ' ') sb.Append(c); }
				int nameLen = sb.Length;
				string ext = "";
				{
					var e = new StringBuilder(3);
					for (int i = 9; i <= 11; i++) { char c = (char)(dir[off + i] & 0x7F); if (c != ' ') e.Append(c); }
					ext = e.ToString();
				}
				if (nameLen == 0 && ext.Length == 0) continue;
				if (!LooksLikeFilename(sb.ToString(), ext)) continue;

				string full = ext.Length > 0 ? sb.ToString() + "." + ext : sb.ToString();
				int records = dir[off + 15];

				if (!byName.TryGetValue(full, out var entry))
				{
					entry = new Entry
					{
						UserNumber = user,
						Name = full,
						ReadOnly = (dir[off + 9] & 0x80) != 0,
						SystemHidden = (dir[off + 10] & 0x80) != 0,
						Archived = (dir[off + 11] & 0x80) != 0,
					};
					byName[full] = entry;
					files.Add(entry);
				}
				// CP/M splits a file across 32-byte extent entries; total size = sum of every extent's records * 128
				entry.SizeBytes += records * 128L;
			}
			return files;
		}

		/// <summary>Convenience: a human-readable multi-line catalogue (empty string if no files).</summary>
		public static string Catalogue(FluxDisk disk)
		{
			var files = Read(disk);
			if (files.Count == 0) return "";
			var sb = new StringBuilder();
			foreach (var f in files) sb.AppendLine(f.ToString());
			return sb.ToString();
		}

		// logical sector 0 = the lowest-numbered sector on track 0 (its data is the +3 disk specification)
		private static byte[] LogicalSector0(FluxDisk disk)
		{
			var t = disk.GetTrack(0, 0);
			if (t == null) return null;
			var secs = StandardMfmFormat.DecodeSectors(t);
			byte[] best = null; int bestR = int.MaxValue;
			foreach (var s in secs)
				if (s.HasData && s.R < bestR) { bestR = s.R; best = s.Data; }
			return best;
		}

		// concatenate a track's sectors in ascending-R (logical) order, up to byteCount bytes
		private static byte[] ReadTrackBytes(FluxDisk disk, int cyl, int byteCount)
		{
			var t = disk.GetTrack(cyl, 0);
			if (t == null) return null;
			var secs = StandardMfmFormat.DecodeSectors(t);
			secs.Sort((a, b) => a.R.CompareTo(b.R));
			var buf = new byte[byteCount];
			int pos = 0;
			foreach (var s in secs)
			{
				if (pos >= byteCount) break;
				if (!s.HasData) continue;
				int n = System.Math.Min(s.Data.Length, byteCount - pos);
				System.Array.Copy(s.Data, 0, buf, pos, n);
				pos += n;
			}
			return pos == 0 ? null : buf;
		}

		private static bool LooksLikeFilename(string name, string ext)
		{
			foreach (var c in name) if (c < 0x20 || c > 0x7E) return false;
			foreach (var c in ext) if (c < 0x20 || c > 0x7E) return false;
			return true;
		}
	}
}
