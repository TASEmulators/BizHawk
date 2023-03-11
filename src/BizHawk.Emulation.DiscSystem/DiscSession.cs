using System.Collections.Generic;

namespace BizHawk.Emulation.DiscSystem
{
	public class DiscSession
	{
		//Notable omission:
			//Length of the session
			//How should this be defined? It's even harder than determining a track length

		/// <summary>
		/// The DiscTOC corresponding to the RawTOCEntries.
		/// </summary>
		public DiscTOC TOC;

		/// <summary>
		/// The raw TOC entries found in the lead-in track.
		/// These aren't very useful, but they're one of the most lowest-level data structures from which other TOC-related stuff is derived
		/// </summary>
		public readonly List<RawTOCEntry> RawTOCEntries = new();

		/// <summary>
		/// The LBA of the session's leadout. In other words, for all intents and purposes, the end of the session
		/// </summary>
		public int LeadoutLBA => LeadoutTrack.LBA;

		/// <summary>
		/// The session number
		/// </summary>
		public int Number;

		/// <summary>
		/// The number of user information tracks in the session.
		/// This excludes the lead-in and lead-out tracks
		/// Use this instead of Tracks.Count
		/// </summary>
		public int InformationTrackCount => Tracks.Count - 2;

		/// <summary>
		/// All the tracks in the session.. but... Tracks[0] is the lead-in track. Tracks[1] should be "Track 1". So beware of this.
		/// For a disc with "3 tracks", Tracks.Count will be 5: it includes that lead-in track as well as the leadout track.
		/// Perhaps we should turn this into a special collection type with no Count or Length, or a method to GetTrack()
		/// </summary>
		public readonly IList<DiscTrack> Tracks = new List<DiscTrack>();

		/// <summary>
		/// A reference to the first information track (Track 1)
		/// The raw TOC may have specified something different; it's not clear how this discrepancy is handled.
		/// </summary>
		public DiscTrack FirstInformationTrack => Tracks[1];

		/// <summary>
		/// A reference to the last information track on the disc.
		/// The raw TOC may have specified something different; it's not clear how this discrepancy is handled.
		/// </summary>
		public DiscTrack LastInformationTrack => Tracks[InformationTrackCount];

		/// <summary>
		/// A reference to the lead-out track.
		/// Effectively, the end of the user area of the disc.
		/// </summary>
		public DiscTrack LeadoutTrack => Tracks[Tracks.Count - 1];

		/// <summary>
		/// A reference to the lead-in track
		/// </summary>
		public DiscTrack LeadinTrack => Tracks[0];

		/// <summary>
		/// Determines which track of the session is at the specified LBA.
		/// </summary>
		public DiscTrack SeekTrack(int lba)
		{
			var ses = this;

			for (var i = 1; i < Tracks.Count; i++)
			{
				var track = ses.Tracks[i];
				//funny logic here: if the current track's LBA is > the requested track number, it means the previous track is the one we wanted
				if (track.LBA > lba)
					return ses.Tracks[i - 1];
			}
			return ses.LeadoutTrack;
		}
#if false
		public class Index
		{
			public int Number;
			public int LBA;
		}
#endif
	}
}