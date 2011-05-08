using System;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;

namespace BizHawk.Disc
{

	public class DiscTOC
	{
		public class Session
		{
			public int num;
			public List<Track> Tracks = new List<Track>();

			//the length of the track (should be the sum of all track lengths)
			public int length_lba;
		}

		public class Track
		{
			public int num;
			public List<Index> Indexes = new List<Index>();

			//the length of the track (should be the sum of all index lengths)
			public int length_lba;
		}

		public class Index
		{
			public int num;
			public int lba;

			//the length of the section
			public int length_lba;
		}

		public static string FormatLBA(int lba)
		{
			return string.Format("{0} ({1:D2}:{2:D2}:{3:D2})", lba, lba / 60 / 75, (lba / 75) % 60, lba % 75);
		}

		public string DebugPrint()
		{
			StringBuilder sb = new StringBuilder();
			foreach (var session in Sessions)
			{
				sb.AppendFormat("SESSION {0:D2} (length={1})\n", session.num, session.length_lba);
				foreach (var track in session.Tracks)
				{
					sb.AppendFormat("  TRACK {0:D2} (length={1})\n", track.num, track.length_lba);
					foreach (var index in track.Indexes)
					{
						sb.AppendFormat("    INDEX {0:D2}: {1}\n", index.num, FormatLBA(index.lba));
					}
				}
				sb.AppendFormat("-EOF-\n");
			}

			return sb.ToString();
		}

		public List<Session> Sessions = new List<Session>();
		public int length_lba;

		public void AnalyzeLengthsFromIndexLengths()
		{
			foreach (var session in Sessions)
			{
				foreach (var track in session.Tracks)
				{
					int track_size = 0;
					foreach (var index in track.Indexes)
						track_size += index.length_lba;
					track.length_lba += track_size;
					session.length_lba += track_size;
					length_lba += track_size;
				}
			}
		}
	}

}