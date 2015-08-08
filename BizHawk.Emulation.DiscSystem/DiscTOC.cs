using System;
using System.Text;
using System.Collections.Generic;

namespace BizHawk.Emulation.DiscSystem
{
	/// <summary>
	/// Represents our best guess at what a disc drive firmware will receive by reading the TOC from the lead-in track, modeled after CCD contents and mednafen/PSX needs.
	/// </summary>
	public class DiscTOC
	{
		/// <summary>
		/// The TOC specifies the first recorded track number, independently of whatever may actually be recorded
		/// </summary>
		public int FirstRecordedTrackNumber = -1;

		/// <summary>
		/// The TOC specifies the last recorded track number, independently of whatever may actually be recorded
		/// </summary>
		public int LastRecordedTrackNumber = -1;

		/// <summary>
		/// The TOC specifies the format of the session, so here it is.
		/// </summary>
		public SessionFormat Session1Format = SessionFormat.None;

		/// <summary>
		/// Information about a single track in the TOC
		/// </summary>
		public struct TOCItem
		{
			/// <summary>
			/// [IEC10149] "the control field used in the information track"
			/// the raw TOC entries do have a control field which is supposed to match what's found in the track.
			/// Determining whether a track contains audio or data is very important. 
			/// A track mode can't be safely determined from reading sectors from the actual track if it's an audio track (there's no sector header with a mode byte)
			/// </summary>
			public EControlQ Control;

			/// <summary>
			/// Whether the Control indicates that this is data
			/// </summary>
			public bool IsData { get { return (Control & EControlQ.DATA) != 0; } }

			/// <summary>
			/// The location of the track (Index 1)
			/// </summary>
			public int LBA;

			/// <summary>
			/// Whether this entry exists (since the table is 101 entries long always)
			/// </summary>
			public bool Exists;
		}

		/// <summary>
		/// This is a convenient format for storing the TOC (taken from mednafen)
		/// Element 0 is the Lead-in track
		/// Element 100 is the Lead-out track
		/// </summary>
		public TOCItem[] TOCItems = new TOCItem[101];

		/// <summary>
		/// The timestamp of the leadout track. In other words, the end of the user area.
		/// </summary>
		public int LeadoutLBA { get { return TOCItems[100].LBA; } }
	}

	

}