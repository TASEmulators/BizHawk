using System;
using System.Text;
using System.Collections.Generic;

namespace BizHawk.Emulation.DiscSystem
{
	/// <summary>
	/// Represents our best guess at what a disc drive firmware will receive by reading the TOC from the lead-in track, modeled after CCD contents and mednafen/PSX needs.
	/// </summary>
	public class DiscTOCRaw
	{
		/// <summary>
		/// Synthesizes the TOC from a set of raw entries.
		/// When a disc drive firmware reads the lead-in area, it builds this TOC from finding q-mode 1 sectors in the Q subchannel of the lead-in area.
		/// Question: I guess it must ignore q-mode != 1? what else would it do with it?
		/// </summary>
		public class SynthesizeFromRawTOCEntriesJob
		{
			public IEnumerable<RawTOCEntry> Entries;
			public List<string> Log = new List<string>();
			public DiscTOCRaw Result;

			public void Run()
			{
				SynthesizeFromRawTOCEntriesJob job = this;
				DiscTOCRaw ret = new DiscTOCRaw();

				//this is a dummy, for convenience in array indexing, so that track 1 is at array index 1
				ret.TOCItems[0].LBATimestamp = new Timestamp(0); //arguably could be -150, but let's not just yet
				ret.TOCItems[0].Control = 0;
				ret.TOCItems[0].Exists = false;

				//just in case this doesnt get set...
				ret.FirstRecordedTrackNumber = 0;
				ret.LastRecordedTrackNumber = 0;

				int maxFoundTrack = 0;

				foreach (var te in job.Entries)
				{
					var q = te.QData;
					var point = q.q_index.DecimalValue;

					//see ECMD-394 page 5-14 for info about point = 0xA0, 0xA1, 0xA2

					if (point == 0x00)
						job.Log.Add("unexpected POINT=00 in lead-in Q-channel");
					else if (point == 255)
						throw new InvalidOperationException("point == 255");
					else if (point <= 99)
					{
						maxFoundTrack = Math.Max(maxFoundTrack, point);
						ret.TOCItems[point].LBATimestamp = new Timestamp(q.AP_Timestamp.Sector - 150); //RawTOCEntries contained an absolute time
						ret.TOCItems[point].Control = q.CONTROL;
						ret.TOCItems[point].Exists = true;
					}
					else if (point == 100) //0xA0 bcd
					{
						ret.FirstRecordedTrackNumber = q.ap_min.DecimalValue;
						if (q.ap_frame.DecimalValue != 0) job.Log.Add("PFRAME should be 0 for POINT=0xA0");
						if (q.ap_sec.DecimalValue == 0x00) ret.Session1Format = DiscTOCRaw.SessionFormat.Type00_CDROM_CDDA;
						else if (q.ap_sec.DecimalValue == 0x10) ret.Session1Format = DiscTOCRaw.SessionFormat.Type10_CDI;
						else if (q.ap_sec.DecimalValue == 0x20) ret.Session1Format = DiscTOCRaw.SessionFormat.Type20_CDXA;
						else job.Log.Add("Unrecognized session format: PSEC should be one of {0x00,0x10,0x20} for POINT=0xA0");
					}
					else if (point == 101) //0xA1 bcd
					{
						ret.LastRecordedTrackNumber = q.ap_min.DecimalValue;
						if (q.ap_sec.DecimalValue != 0) job.Log.Add("PSEC should be 0 for POINT=0xA1");
						if (q.ap_frame.DecimalValue != 0) job.Log.Add("PFRAME should be 0 for POINT=0xA1");
					}
					else if (point == 102) //0xA2 bcd
					{
						ret.TOCItems[100].LBATimestamp = new Timestamp(q.AP_Timestamp.Sector - 150); //RawTOCEntries contained an absolute time
						ret.TOCItems[100].Control = 0; //not clear what this should be
						ret.TOCItems[100].Exists = true;
					}
				}

				//this is speculative:
				//well, nothing to be done here..
				if (ret.FirstRecordedTrackNumber == -1) { }
				if (ret.LastRecordedTrackNumber == -1) { ret.LastRecordedTrackNumber = maxFoundTrack; }
				if (ret.Session1Format == SessionFormat.None) ret.Session1Format = SessionFormat.Type00_CDROM_CDDA;
				
				//if (!ret.LeadoutTimestamp.Valid) { 
				//  //we're DOOMED. we cant know the length of the last track without this....
				//}
				job.Result = ret;
			}
		}

		public enum SessionFormat
		{
			None = -1,
			Type00_CDROM_CDDA = 0x00,
			Type10_CDI = 0x10,
			Type20_CDXA = 0x20
		}

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
			public Timestamp LBATimestamp;

			/// <summary>
			/// Whether this entry exists (since the table is 101 entries long always)
			/// </summary>
			public bool Exists;
		}

		/// <summary>
		/// This is a convenient format for storing the TOC (taken from mednafen)
		/// Index 0 is empty, so that track 1 is in index 1.
		/// Index 100 is the Lead-out track
		/// </summary>
		public TOCItem[] TOCItems = new TOCItem[101];

		/// <summary>
		/// The timestamp of the leadout track. In other words, the end of the user area.
		/// </summary>
		public Timestamp LeadoutLBA { get { return TOCItems[100].LBATimestamp; } }
	}
}