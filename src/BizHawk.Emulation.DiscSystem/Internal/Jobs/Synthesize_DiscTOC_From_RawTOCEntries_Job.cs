using System.Collections.Generic;

namespace BizHawk.Emulation.DiscSystem
{
	/// <summary>
	/// Synthesizes the TOC from a set of raw entries.
	/// When a disc drive firmware reads the lead-in area, it builds this TOC from finding q-mode 1 sectors in the Q subchannel of the lead-in area.
	/// Question: I guess it must ignore q-mode != 1? what else would it do with it?
	/// </summary>
	internal class Synthesize_DiscTOC_From_RawTOCEntries_Job
	{
		private readonly IReadOnlyList<RawTOCEntry> Entries;

		public Synthesize_DiscTOC_From_RawTOCEntries_Job(IReadOnlyList<RawTOCEntry> entries) => Entries = entries;

		public DiscTOC Result { get; private set; }

		private readonly List<string> Log = new();

		public IReadOnlyList<string> GetLog() => Log;

		public void Run()
		{
			Result = new();

			//this is a dummy, for convenience in array indexing, so that track 1 is at array index 1
			Result.TOCItems[0].LBA = 0; //arguably could be -150, but let's not just yet
			Result.TOCItems[0].Control = 0;
			Result.TOCItems[0].Exists = false;

			var minFoundTrack = 100;
			var maxFoundTrack = 1;

			foreach (var te in Entries)
			{
				var q = te.QData;
				var point = q.q_index.DecimalValue;

				//see ECMD-394 page 5-14 for info about point = 0xA0, 0xA1, 0xA2

				switch (point)
				{
					case 0x00:
						Log.Add("unexpected POINT=00 in lead-in Q-channel");
						break;
					case 255:
						throw new InvalidOperationException("point == 255");
					case <= 99:
						minFoundTrack = Math.Min(minFoundTrack, point);
						maxFoundTrack = Math.Max(maxFoundTrack, point);
						Result.TOCItems[point].LBA = q.AP_Timestamp - 150; //RawTOCEntries contained an absolute time
						Result.TOCItems[point].Control = q.CONTROL;
						Result.TOCItems[point].Exists = true;
						break;
					//0xA0 bcd
					case 100:
					{
						Result.FirstRecordedTrackNumber = q.ap_min.DecimalValue;
						if (q.ap_frame.DecimalValue != 0) Log.Add("PFRAME should be 0 for POINT=0xA0");
						switch (q.ap_sec.DecimalValue)
						{
							case 0x00:
								Result.SessionFormat = SessionFormat.Type00_CDROM_CDDA;
								break;
							case 0x10:
								Result.SessionFormat = SessionFormat.Type10_CDI;
								break;
							case 0x20:
								Result.SessionFormat = SessionFormat.Type20_CDXA;
								break;
							default:
								Log.Add("Unrecognized session format: PSEC should be one of {0x00,0x10,0x20} for POINT=0xA0");
								break;
						}

						break;
					}
					//0xA1 bcd
					case 101:
					{
						Result.LastRecordedTrackNumber = q.ap_min.DecimalValue;
						if (q.ap_sec.DecimalValue != 0) Log.Add("PSEC should be 0 for POINT=0xA1");
						if (q.ap_frame.DecimalValue != 0) Log.Add("PFRAME should be 0 for POINT=0xA1");
						break;
					}
					//0xA2 bcd
					case 102:
						Result.TOCItems[100].LBA = q.AP_Timestamp - 150; //RawTOCEntries contained an absolute time
						Result.TOCItems[100].Control = 0; //not clear what this should be
						Result.TOCItems[100].Exists = true;
						break;
				}
			}

			//this is speculative:
			if (Result.FirstRecordedTrackNumber == -1) { Result.FirstRecordedTrackNumber = minFoundTrack == 100 ? 1 : minFoundTrack; }
			if (Result.LastRecordedTrackNumber == -1) { Result.LastRecordedTrackNumber = maxFoundTrack; }
			if (Result.SessionFormat == SessionFormat.None) Result.SessionFormat = SessionFormat.Type00_CDROM_CDDA;

			//if (!Result.LeadoutTimestamp.Valid) {
			//  //we're DOOMED. we cant know the length of the last track without this....
			//}
		}
	}
}