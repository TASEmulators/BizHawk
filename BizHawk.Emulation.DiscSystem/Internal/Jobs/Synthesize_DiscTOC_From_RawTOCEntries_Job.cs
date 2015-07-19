using System;
using System.Collections.Generic;

namespace BizHawk.Emulation.DiscSystem
{
	/// <summary>
	/// Synthesizes the TOC from a set of raw entries.
	/// When a disc drive firmware reads the lead-in area, it builds this TOC from finding q-mode 1 sectors in the Q subchannel of the lead-in area.
	/// Question: I guess it must ignore q-mode != 1? what else would it do with it?
	/// </summary>
	class Synthesize_DiscTOC_From_RawTOCEntries_Job
	{
		public IEnumerable<RawTOCEntry> Entries;
		public List<string> Log = new List<string>();
		public DiscTOC Result;

		public void Run()
		{
			var job = this;
			DiscTOC ret = new DiscTOC();

			//this is a dummy, for convenience in array indexing, so that track 1 is at array index 1
			ret.TOCItems[0].LBA = 0; //arguably could be -150, but let's not just yet
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
					ret.TOCItems[point].LBA = q.AP_Timestamp - 150; //RawTOCEntries contained an absolute time
					ret.TOCItems[point].Control = q.CONTROL;
					ret.TOCItems[point].Exists = true;
				}
				else if (point == 100) //0xA0 bcd
				{
					ret.FirstRecordedTrackNumber = q.ap_min.DecimalValue;
					if (q.ap_frame.DecimalValue != 0) job.Log.Add("PFRAME should be 0 for POINT=0xA0");
					if (q.ap_sec.DecimalValue == 0x00) ret.Session1Format = SessionFormat.Type00_CDROM_CDDA;
					else if (q.ap_sec.DecimalValue == 0x10) ret.Session1Format = SessionFormat.Type10_CDI;
					else if (q.ap_sec.DecimalValue == 0x20) ret.Session1Format = SessionFormat.Type20_CDXA;
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
					ret.TOCItems[100].LBA = q.AP_Timestamp - 150; //RawTOCEntries contained an absolute time
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
}