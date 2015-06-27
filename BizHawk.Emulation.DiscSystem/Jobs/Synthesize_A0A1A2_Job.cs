using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Generic;

//TODO - generate correct Q subchannel CRC

namespace BizHawk.Emulation.DiscSystem
{
	/// <summary>
	/// Synthesizes RawTCOEntry A0 A1 A2 from the provided information.
	/// This might be reused by formats other than CUE later, so it isn't directly associated with that
	/// </summary>
	public class Synthesize_A0A1A2_Job
	{
		/// <summary>
		/// "First Recorded Track Number" value for TOC (usually 1)
		/// </summary>
		public int IN_FirstRecordedTrackNumber;

		/// <summary>
		/// "Last Recorded Track Number" value for TOC
		/// </summary>
		public int IN_LastRecordedTrackNumber;

		/// <summary>
		/// The absolute timestamp of the lead-out track
		/// </summary>
		public Timestamp IN_LeadoutTimestamp;

		/// <summary>
		/// The session format for this TOC
		/// </summary>
		public DiscTOCRaw.SessionFormat IN_Session1Format;

		/// <summary>
		/// Appends the new entries to the provided list
		/// </summary>
		public void Run(List<RawTOCEntry> entries)
		{
			SubchannelQ sq = new SubchannelQ();

			BufferedSubcodeSector bss = new BufferedSubcodeSector(); //TODO - its hacky that we need this.. (NOT USED YET)

			//ADR (q-Mode) is necessarily 0x01 for a RawTOCEntry
			const int kADR = 1;
			const int kUnknownControl = 0;

			sq.SetStatus(kADR, (EControlQ)kUnknownControl);

			//first recorded track number:
			sq.q_index.BCDValue = 0xA0;
			sq.ap_min.DecimalValue = IN_FirstRecordedTrackNumber;
			switch(IN_Session1Format)
			{
				//TODO these probably shouldn't be decimal values
				case DiscTOCRaw.SessionFormat.Type00_CDROM_CDDA: sq.ap_sec.DecimalValue = 0x00; break;
				case DiscTOCRaw.SessionFormat.Type10_CDI: sq.ap_sec.DecimalValue = 0x10; break;
				case DiscTOCRaw.SessionFormat.Type20_CDXA: sq.ap_sec.DecimalValue = 0x20; break;
				default: throw new InvalidOperationException("Invalid Session1Format");
			}
			sq.ap_frame.DecimalValue = 0;

			entries.Add(new RawTOCEntry { QData = sq });

			//last recorded track number:
			sq.q_index.BCDValue = 0xA1;
			sq.ap_min.DecimalValue = IN_LastRecordedTrackNumber;
			sq.ap_sec.DecimalValue = 0;
			sq.ap_frame.DecimalValue = 0;

			entries.Add(new RawTOCEntry { QData = sq });

			//leadout:
			sq.q_index.BCDValue = 0xA2;
			sq.AP_Timestamp = IN_LeadoutTimestamp;
			

			entries.Add(new RawTOCEntry { QData = sq });
		}
	}
}