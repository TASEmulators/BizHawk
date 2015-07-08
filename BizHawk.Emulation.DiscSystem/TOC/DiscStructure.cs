using System;
using System.Text;
using System.Collections.Generic;

namespace BizHawk.Emulation.DiscSystem
{
	/// <summary>
	/// Contains structural information for the disc broken down into c# data structures for easy interrogation.
	/// This represents a best-effort interpretation of the raw disc image.
	/// NOTE: Since this ended up really just having the list of sessions.. maybe it isn't needed and can just float on up into Disc
	/// </summary>
	public class DiscStructure
	{
		/// <summary>
		/// This is a 0-indexed list of sessions (session 1 is at [0])
		/// Support for multiple sessions is thoroughly not working yet
		/// TODO - make re-index me with a null session 0
		/// </summary>
		public List<Session> Sessions = new List<Session>();


		/// <summary>
		/// Determines which track of session 1 is at the specified LBA.
		/// Returns null if it's before track 1
		/// </summary>
		public Track SeekTrack(int lba)
		{
			var ses = Sessions[0];
			
			//take care with this loop bounds:
			for (int i = 1; i <= ses.InformationTrackCount; i++)
			{
				var track = ses.Tracks[i];
				if (track.LBA > lba)
					return (i==1)?null:ses.Tracks[i];
			}
			return ses.Tracks[ses.Tracks.Count];
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
				Result.Sessions.Add(null); //placeholder session for reindexing
				Result.Sessions.Add(session);

				session.Number = 1;
				
				if(TOCRaw.FirstRecordedTrackNumber != 1)
					throw new InvalidOperationException("Unsupported: FirstRecordedTrackNumber != 1");

				//add a lead-in track
				session.Tracks.Add(new DiscStructure.Track() {
					Number = 0,
					Control = EControlQ.None, //TODO - not accurate (take from track 1?)
					LBA = -150 //TODO - not accurate
				});

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

					//determine track length according to... how? It isn't clear.
					//Let's not do this until it's needed.
					//if (i == ntracks - 1)
					//  track.Length = TOCRaw.LeadoutLBA.Sector - track.LBA;
					//else track.Length = (TOCRaw.TOCItems[i + 2].LBATimestamp.Sector - track.LBA);
				}

				//add lead-out track
				session.Tracks.Add(new DiscStructure.Track()
				{
					Number = 0xA0, //right?
					Control = EControlQ.None, //TODO - not accurate (take from track 1?)
					LBA = TOCRaw.LeadoutLBA.Sector
				});

				//link track list 
				for (int i = 0; i < session.Tracks.Count - 1; i++)
				{
					session.Tracks[i].NextTrack = session.Tracks[i + 1];
				}

				//other misc fields
				session.InformationTrackCount = session.Tracks.Count - 2;
			}
		}

		public class Session
		{
			//Notable omission:
				//Length of the session
				//How should this be defined? It's even harder than determining a track length

			/// <summary>
			/// The LBA of the session's leadout. In other words, for all intents and purposes, the end of the session
			/// </summary>
			public int LeadoutLBA { get { return LeadoutTrack.LBA; } }

			/// <summary>
			/// The session number
			/// </summary>
			public int Number;

			/// <summary>
			/// The number of user information tracks in the session.
			/// This excludes track 0 and the lead-out track.
			/// Use this instead of Tracks.Count
			/// </summary>
			public int InformationTrackCount;

			/// <summary>
			/// All the tracks in the session.. but... Tracks[0] is the lead-in track placeholder. Tracks[1] should be "Track 1". So beware of this.
			/// For a disc with "3 tracks", Tracks.Count will be 5: it includes that lead-in track as well as the leadout track.
			/// Perhaps we should turn this into a special collection type with no Count or Length, or a method to GetTrack()
			/// </summary>
			public List<Track> Tracks = new List<Track>();

			/// <summary>
			/// A reference to the first information track (Track 1)
			/// </summary>
			public Track FirstInformationTrack { get { return Tracks[1]; } }

			/// <summary>
			/// A reference to the lead-out track.
			/// Effectively, the end of the user area of the disc.
			/// </summary>
			public Track LeadoutTrack { get { return Tracks[Tracks.Count - 1]; } }
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
				//(note: a CCD should contain indices, but it's not clear whether it's required. logically it shouldnt be)
			//Notable omission:
				//Length of the track.
				//How should this be defined? Between which indices? It's really hard.

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
			/// The next track in the session. null for the leadout track of a session.
			/// </summary>
			public Track NextTrack;
		}

		public class Index
		{
			public int Number;
			public int LBA;
		}

	}

}