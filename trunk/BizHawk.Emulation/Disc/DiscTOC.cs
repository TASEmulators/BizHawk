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

			//the length of the session (should be the sum of all track lengths)
			public int length_lba;
			public Cue.CueTimestamp FriendlyLength { get { return new Cue.CueTimestamp(length_lba); } }
		}

		public class Track
		{
			public ETrackType TrackType;
			public int num;
			public List<Index> Indexes = new List<Index>();

			//the length of the track (should be the sum of all index lengths)
			public int length_lba;
			public Cue.CueTimestamp FriendlyLength { get { return new Cue.CueTimestamp(length_lba); } }
		}

		public class Index
		{
			public int num;
			public int lba;

			//the length of the section
			//public int length_lba;
			//public Cue.CueTimestamp FriendlyLength { get { return new Cue.CueTimestamp(length_lba); } }
		}

		public string GenerateCUE(CueBinPrefs prefs)
		{
			StringBuilder sb = new StringBuilder();
			foreach (var session in Sessions)
			{
				if (!prefs.SingleSession)
				{
					//dont want to screw around with sessions for now
					if (prefs.AnnotateCue) sb.AppendFormat("SESSION {0:D2} (length={1})\n", session.num, session.length_lba);
					else sb.AppendFormat("SESSION {0:D2}\n", session.num);
				}
				foreach (var track in session.Tracks)
				{
					if (prefs.AnnotateCue) sb.AppendFormat("  TRACK {0:D2} {1} (length={2})\n", track.num, Cue.TrackTypeStringForTrackType(track.TrackType), track.length_lba);
					else sb.AppendFormat("  TRACK {0:D2} {1}\n", track.num, Cue.TrackTypeStringForTrackType(track.TrackType));
					foreach (var index in track.Indexes)
					{
						//if (prefs.PreferPregapCommand && index.num == 0)
						//    sb.AppendFormat("    PREGAP {0}\n", new Cue.CueTimestamp(index.length_lba).Value);
						//else 
						sb.AppendFormat("    INDEX {0:D2} {1}\n", index.num, new Cue.CueTimestamp(index.lba).Value);
					}
				}
			}

			return sb.ToString();
		}

		public List<Session> Sessions = new List<Session>();
		public int length_lba;

		public void AnalyzeLengthsFromIndexLengths()
		{
			//this is a little more complex than it looks, because the length of a thing is not determined by summing it
			//but rather by the difference in lbas between start and end
			length_lba = 0;
			foreach (var session in Sessions)
			{
				var firstTrack = session.Tracks[0];
				var lastTrack = session.Tracks[session.Tracks.Count - 1];
				session.length_lba = lastTrack.Indexes[0].lba + lastTrack.length_lba - firstTrack.Indexes[0].lba;
				length_lba += session.length_lba;
			}
		}
	}

}