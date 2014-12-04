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
		/// When a disc drive firmware reads the lead-in area, it builds this TOC from finding ADR=1 (mode=1) sectors in the Q subchannel of the lead-in area.
		/// I don't think this lead-in area Q subchannel is stored in a CCD .sub file.
		/// The disc drive firmware will discover other mode sectors in the lead-in area, and it will register those in separate data structures.
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

				//just in case this doesnt get set...
				ret.FirstRecordedTrackNumber = 0;
				ret.LastRecordedTrackNumber = 0;

				int maxFoundTrack = 0;

				foreach (var te in job.Entries)
				{
					var q = te.QData;
					int point = q.q_index;

					//see ECMD-394 page 5-14 for info about point = 0xA0, 0xA1, 0xA2

					if (point == 0x00)
						job.Log.Add("unexpected POINT=00 in lead-in Q-channel");
					else if (point <= 99)
					{
						maxFoundTrack = Math.Max(maxFoundTrack, point);
						ret.TOCItems[point].LBATimestamp = q.AP_Timestamp;
						ret.TOCItems[point].Control = q.CONTROL;
						ret.TOCItems[point].Exists = true;
					}
					else if (point == 0xA0)
					{
						ret.FirstRecordedTrackNumber = q.ap_min.DecimalValue;
						if (q.ap_frame.DecimalValue != 0) job.Log.Add("PFRAME should be 0 for POINT=0xA0");
						if (q.ap_sec.DecimalValue == 0x00) ret.Session1Format = DiscTOCRaw.SessionFormat.Type00_CDROM_CDDA;
						else if (q.ap_sec.DecimalValue == 0x10) ret.Session1Format = DiscTOCRaw.SessionFormat.Type10_CDI;
						else if (q.ap_sec.DecimalValue == 0x20) ret.Session1Format = DiscTOCRaw.SessionFormat.Type20_CDXA;
						else job.Log.Add("Unrecognized session format: PSEC should be one of {0x00,0x10,0x20} for POINT=0xA0");
					}
					else if (point == 0xA1)
					{
						ret.LastRecordedTrackNumber = q.ap_min.DecimalValue;
						if (q.ap_sec.DecimalValue != 0) job.Log.Add("PSEC should be 0 for POINT=0xA1");
						if (q.ap_frame.DecimalValue != 0) job.Log.Add("PFRAME should be 0 for POINT=0xA1");
					}
					else if (point == 0xA2)
					{
						ret.LeadoutTimestamp = q.AP_Timestamp;
					}
				}

				//this is speculative:
				//well, nothing to be done here..
				if (ret.FirstRecordedTrackNumber == 0) { }
				if (ret.LastRecordedTrackNumber == 0) { ret.LastRecordedTrackNumber = maxFoundTrack; }
				job.Result = ret;
			}
		}

		public enum SessionFormat
		{
			Type00_CDROM_CDDA,
			Type10_CDI,
			Type20_CDXA
		}

		/// <summary>
		/// The TOC specifies the first recorded track number, independently of whatever may actually be recorded (its unclear what happens if theres a mismatch)
		/// </summary>
		public int FirstRecordedTrackNumber;

		/// <summary>
		/// The TOC specifies the last recorded track number, independently of whatever may actually be recorded (its unclear what happens if theres a mismatch)
		/// </summary>
		public int LastRecordedTrackNumber;

		/// <summary>
		/// The TOC specifies the format of the session, so here it is.
		/// </summary>
		public SessionFormat Session1Format = SessionFormat.Type00_CDROM_CDDA;

		public struct TOCItem
		{
			public EControlQ Control;
			public Timestamp LBATimestamp;
			public bool Exists;
		}

		/// <summary>
		/// I think it likely that a firmware would just have a buffer for 100 of these. There can never be more than 100 and it might not match the 0xA0 and 0xA1 -specified values
		/// Also #0 is illegal and is always empty.
		/// </summary>
		public TOCItem[] TOCItems = new TOCItem[100];

		/// <summary>
		/// POINT=0xA2 specifies this
		/// </summary>
		public Timestamp LeadoutTimestamp;
	}
}