using System;
using System.Text;
using System.Collections.Generic;

namespace BizHawk.Emulation.DiscSystem
{
	/// <summary>
	/// Contains structural information for the disc broken down into c# data structures for easy interrogation.
	/// This represents a best-effort interpretation of the raw disc image.
	/// </summary>
	public class DiscStructure
	{
		/// <summary>
		/// This is a 0-indexed list of sessions (session 1 is at [0])
		/// Support for multiple sessions is thoroughly not working yet
		/// </summary>
		public List<Session> Sessions = new List<Session>();

		/// <summary>
		/// How many sectors in the disc, including the 150 lead-in sectors, up to the end of the last track (before the lead-out track)
		/// TODO - does anyone ever need this as the ABA Count? Rename it LBACount or ABACount
		/// </summary>
		public int LengthInSectors;

		/// <summary>
		/// Length (including lead-in) of the disc as a timestamp
		/// TODO - does anyone ever need this as the ABA Count? Rename it LBACount or ABACount
		/// </summary>
		public Timestamp FriendlyLength { get { return new Timestamp(LengthInSectors); } }

		/// <summary>
		/// How many bytes of data in the disc (including lead-in). Disc sectors are really 2352 bytes each, so this is LengthInSectors * 2352
		/// TODO - this is garbage
		/// </summary>
		public long BinarySize
		{
			get { return LengthInSectors * 2352; }
		}

		/// <summary>
		/// Determines which track of session 0 is at the specified LBA.
		/// Returns null if it's before track 1
		/// </summary>
		public Track SeekTrack(int lba)
		{
			var ses = Sessions[0];
			for (int i = 0; i < ses.Tracks.Count; i++)
			{
				var track = ses.Tracks[i];
				if (track.LBA > lba)
					return (i==0)?null:ses.Tracks[i - 1];
			}
			return ses.Tracks[ses.Tracks.Count - 1];
		}

		///// <summary>
		///// Synthesizes the DiscStructure from RawTOCEntriesJob
		///// </summary>
		//public class SynthesizeFromRawTOCEntriesJob
		//{
		//  public IEnumerable<RawTOCEntry> Entries;
		//  public DiscStructure Result;

		//  public void Run()
		//  {
		//    Result = new DiscStructure();
		//    var session = new Session();
		//    Result.Sessions.Add(session);

		//    //TODO - are these necessarily in order?
		//    foreach (var te in Entries)
		//    {
		//      int pt = te.QData.q_index.DecimalValue;
		//      int lba = te.QData.Timestamp.Sector;
		//      var bcd2 = new BCD2 { BCDValue = (byte)pt };
		//      if (bcd2.DecimalValue > 99) //A0 A1 A2 leadout and crap
		//        continue;
		//      var track = new Track { LBA = lba, Number = pt };
		//      session.Tracks.Add(track);
		//    }
		//  }
		//}

		public class SynthesizeFromTOCRawJob
		{
			public Disc IN_Disc;
			public DiscTOCRaw TOCRaw;
			public DiscStructure Result;

			public void Run()
			{
				var dsr = new DiscSectorReader(IN_Disc);
				dsr.Policy.DeterministicClearBuffer = false;

				Result = new DiscStructure();
				var session = new Session();
				Result.Sessions.Add(session);

				session.Number = 1;
				
				if(TOCRaw.FirstRecordedTrackNumber != 1)
					throw new InvalidOperationException("Unsupported: FirstRecordedTrackNumber != 1");

				int ntracks = TOCRaw.LastRecordedTrackNumber - TOCRaw.FirstRecordedTrackNumber + 1;
				for(int i=0;i<ntracks;i++)
				{
					var item = TOCRaw.TOCItems[i+1];
					var track = new DiscStructure.Track() { 
						Number = i+1,
						Control = item.Control,
						LBA = item.LBATimestamp.Sector
					};
					session.Tracks.Add(track);

					if (!item.IsData)
						track.Mode = 0;
					else
					{
						//determine the mode by a hardcoded heuristic: check mode of first sector
						track.Mode = dsr.ReadLBA_Mode(track.LBA);
					}

					//determine track length according to law specified in comments for track length
					if (i == ntracks - 1)
						track.Length = TOCRaw.LeadoutLBA.Sector - track.LBA;
					else track.Length = (TOCRaw.TOCItems[i + 2].LBATimestamp.Sector - track.LBA);
				}
			}
		}

		public class Session
		{
			/// <summary>
			/// The session number
			/// </summary>
			public int Number;

			/// <summary>
			/// All the tracks in the session.. but... Tracks[0] should be "Track 1". So beware of this.
			/// Tracks.Count will be good for counting the useful user information tracks on the disc.
			/// </summary>
			public List<Track> Tracks = new List<Track>();
			//I've thought about how to solve this, but it's not easy.
			//At some point we may need to add a true track 0, too.
			//Ideas: Dictionary, or a separate PhysicalTracks and UserTracks list, or add a null and make all loops just cope with that
			//But, the DiscStructure is kind of weak. It might be better to just optimize it for end-users
			//It seems that the current end-users are happy with tracks being implemented the way it is

			//removed:
			////the length of the session (should be the sum of all track lengths)
			//public int length_aba;
		}

		/// <summary>
		/// The Type of a track as specified in the TOC Q-Subchannel data from the control flags.
		/// Could also be 4-Channel Audio, but we'll handle that later if needed
		/// </summary>
		public enum ETrackType
		{
			/// <summary>
			/// The track type isn't always known.. it can take this value til its populated
			/// </summary>
			Unknown,

			/// <summary>
			/// Data track( TOC Q control 0x04 flag set )
			/// </summary>
			Data,

			/// <summary>
			/// Audio track( TOC Q control 0x04 flag clear )
			/// </summary>
			Audio
		}

		/// <summary>
		/// Information about a Track.
		/// </summary>
		public class Track
		{
			//Notable omission: 
				//a list of Indices. It's difficult to reliably construct it.
				//Notably, mednafen can't readily produce it.
				//Indices may need scanning sector by sector. 
				//It's unlikely that any software would be needing indices anyway.
				//We should add another index scanning service if that's ever needed.
				//(note: a CCD should contain indices, but it's not clear whether it's required. logically it wshouldnt be)
			//Notable omission:
				//Mode (0,1,2) 
				//Modes 1 and 2 can't be generally distinguished. 
				//It's a relatively easy heuristic, though: just read the first sector of each track.

			//These omissions could be handled by ReadStructure() policies which permit the scanning of the entire disc.
			//After that, they could be cached in here.
			
			/// <summary>
			/// The number of the track (1-indexed)
			/// </summary>
			public int Number;

			/// <summary>
			/// The Mode of the track (0 is Audio, 1 and 2 are data)
			/// This is heuristically determined.
			/// Actual sector contents may vary
			/// </summary>
			public int Mode;

			/// <summary>
			/// Is this track a Data track?
			/// </summary>
			public bool IsData { get { return !IsAudio; } }

			/// <summary>
			/// Is this track an Audio track?
			/// </summary>
			public bool IsAudio { get { return Mode == 0; } }

			/// <summary>
			/// The 'control' properties of the track indicated by the subchannel Q.
			/// This is as indicated by the disc TOC. 
			/// Actual sector contents may vary.
			/// </summary>
			public EControlQ Control;

			/// <summary>
			/// The starting LBA of the track (index 1).
			/// </summary>
			public int LBA;

			/// <summary>
			/// The length of the track, counted from its index 1 to the next track.
			/// TODO - Shouldn't it exclude the post-gap?
			/// NO - in at least one place (CDAudio) this is used.. and.. it should probably play through the post-gap
			/// That just goes to show how ill-defined this concept is
			/// </summary>
			public int Length;

			///// <summary>
			///// The length as a timestamp (for accessing as a MM:SS:FF)
			///// </summary>
			//public Timestamp FriendlyLength { get { return new Timestamp(Length); } }
		}

		public class Index
		{
			public int Number;
			public int LBA;
		}

		//public void AnalyzeLengthsFromIndexLengths()
		//{
		//  //this is a little more complex than it looks, because the length of a thing is not determined by summing it
		//  //but rather by the difference in lbas between start and end
		//  LengthInSectors = 0;
		//  foreach (var session in Sessions)
		//  {
		//    var firstTrack = session.Tracks[0];
		//    var lastTrack = session.Tracks[session.Tracks.Count - 1];
		//    session.length_aba = lastTrack.Indexes[0].aba + lastTrack.Length - firstTrack.Indexes[0].aba;
		//    LengthInSectors += session.length_aba;
		//  }
		//}
	}

}