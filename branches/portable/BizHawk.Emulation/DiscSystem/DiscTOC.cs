using System;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;

namespace BizHawk.DiscSystem
{

	public class DiscTOC
	{
		/// <summary>
		/// Sessions contained in the disc. Right now support for anything other than 1 session is totally not working
		/// </summary>
		public List<Session> Sessions = new List<Session>();

		/// <summary>
		/// this is an unfinished concept of "TOC Points" which is sometimes more convenient way for organizing the disc contents
		/// </summary>
		public List<TOCPoint> Points = new List<TOCPoint>();

		/// <summary>
		/// Todo - comment about what this actually means
		/// TODO - this is redundant with Sectors.Count
		/// </summary>
		public int length_aba;

		/// <summary>
		/// todo - comment about what this actually means
		/// </summary>
		public Timestamp FriendlyLength { get { return new Timestamp(length_aba); } }

		/// <summary>
		/// todo - comment about what this actually means
		/// </summary>
		public long BinarySize
		{
			get { return length_aba * 2352; }
		}

		/// <summary>
		/// seeks the point immediately before (or equal to) this LBA
		/// </summary>
		public TOCPoint SeekPoint(int lba)
		{
			int aba = lba + 150;
			for(int i=0;i<Points.Count;i++)
			{
				TOCPoint tp = Points[i];
				if (tp.ABA > aba)
					return Points[i - 1];
			}
			return Points[Points.Count - 1];
		}


		/// <summary>
		/// 
		/// </summary>
		public class TOCPoint
		{
			public int Num;
			public int ABA, TrackNum, IndexNum;
			public Track Track;

			public int LBA
			{
				get { return ABA - 150; }
			}
		}


		/// <summary>
		/// Generates the Points list from the current TOC
		/// </summary>
		public void GeneratePoints()
		{
			int num = 0;
			Points.Clear();
			foreach (var ses in Sessions)
			{
				foreach (var track in ses.Tracks)
					foreach (var index in track.Indexes)
					{
						var tp = new TOCPoint();
						tp.Num = num++;
						tp.ABA = index.aba;
						tp.TrackNum = track.num;
						tp.IndexNum = index.num;
						tp.Track = track;
						Points.Add(tp);
					}

				var tpLeadout = new TOCPoint();
				var lastTrack = ses.Tracks[ses.Tracks.Count - 1];
				tpLeadout.Num = num++;
				tpLeadout.ABA = lastTrack.Indexes[1].aba + lastTrack.length_aba;
				tpLeadout.IndexNum = 0;
				tpLeadout.TrackNum = 100;
				tpLeadout.Track = null; //no leadout track.. now... or ever?
				Points.Add(tpLeadout);
			}
		}

		public class Session
		{
			public int num;
			public List<Track> Tracks = new List<Track>();

			//the length of the session (should be the sum of all track lengths)
			public int length_aba;
			public Timestamp FriendlyLength { get { return new Timestamp(length_aba); } }
		}

		public class Track
		{
			public ETrackType TrackType;
			public int num;
			public List<Index> Indexes = new List<Index>();

			/// <summary>
			/// a track logically starts at index 1. 
			/// so this is the length from this index 1 to the next index 1 (or the end of the disc)
			/// the time before track 1 index 1 is the lead-in and isn't accounted for in any track...
			/// </summary>
			public int length_aba;
			public Timestamp FriendlyLength { get { return new Timestamp(length_aba); } }
		}

		public class Index
		{
			public int num;
			public int aba;

			public int LBA
			{
				get { return aba - 150; }
			}

			//the length of the section
			//HEY! This is commented out because it is a bad idea.
			//The length of a section is almost useless, and if you want it, you are probably making an error.
			//public int length_lba;
			//public Cue.Timestamp FriendlyLength { get { return new Cue.Timestamp(length_lba); } }
		}

		public string GenerateCUE_OneBin(CueBinPrefs prefs)
		{
			if (prefs.OneBlobPerTrack) throw new InvalidOperationException("OneBinPerTrack passed to GenerateCUE_OneBin");

			//this generates a single-file cue!!!!!!! dont expect it to generate bin-per-track!
			StringBuilder sb = new StringBuilder();

			foreach (var session in Sessions)
			{
				if (!prefs.SingleSession)
				{
					//dont want to screw around with sessions for now
					if (prefs.AnnotateCue) sb.AppendFormat("SESSION {0:D2} (length={1})\n", session.num, session.length_aba);
					else sb.AppendFormat("SESSION {0:D2}\n", session.num);
				}

				foreach (var track in session.Tracks)
				{
					ETrackType trackType = track.TrackType;

					//mutate track type according to our principle of canonicalization 
					if (trackType == ETrackType.Mode1_2048 && prefs.DumpECM)
						trackType = ETrackType.Mode1_2352;

					if (prefs.AnnotateCue) sb.AppendFormat("  TRACK {0:D2} {1} (length={2})\n", track.num, Cue.TrackTypeStringForTrackType(trackType), track.length_aba);
					else sb.AppendFormat("  TRACK {0:D2} {1}\n", track.num, Cue.TrackTypeStringForTrackType(trackType));
					foreach (var index in track.Indexes)
					{
						//cue+bin has an implicit 150 sector pregap which neither the cue nor the bin has any awareness of
						//except for the baked-in sector addressing.
						//but, if there is an extra-long pregap, we want to reflect it this way
						int lba = index.aba - 150;
						if (lba <= 0 && index.num == 0 && track.num == 1)
						{
						}
						//dont emit index 0 when it is the same as index 1, it is illegal for some reason
						else if (index.num == 0 && index.aba == track.Indexes[1].aba)
						{
							//dont emit index 0 when it is the same as index 1, it confuses some cue parsers
						}
						else
						{
							sb.AppendFormat("    INDEX {0:D2} {1}\n", index.num, new Timestamp(lba).Value);
						}
					}
				}
			}

			return sb.ToString();
		}


		public void AnalyzeLengthsFromIndexLengths()
		{
			//this is a little more complex than it looks, because the length of a thing is not determined by summing it
			//but rather by the difference in lbas between start and end
			length_aba = 0;
			foreach (var session in Sessions)
			{
				var firstTrack = session.Tracks[0];
				var lastTrack = session.Tracks[session.Tracks.Count - 1];
				session.length_aba = lastTrack.Indexes[0].aba + lastTrack.length_aba - firstTrack.Indexes[0].aba;
				length_aba += session.length_aba;
			}
		}
	}

}