using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace BizHawk.Emulation.DiscSystem
{
	/// <summary>
	/// Represents a TOC entry discovered in the Q subchannel data of the lead-in track by the reader. These are stored redundantly.
	/// It isn't clear whether we need anything other than the SubchannelQ data, so I abstracted this in case we need it.
	/// </summary>
	public class RawTOCEntry
	{
		public SubchannelQ QData;
	}

	public enum DiscInterface
	{
		BizHawk, MednaDisc, LibMirage
	}

	/// <summary>
	/// Synthesizes RawTCOEntry A0 A1 A2 from the provided information
	/// TODO - dont these need to have timestamps too?
	/// </summary>
	public class Synthesize_A0A1A2_Job
	{
		//heres a thing
		//https://books.google.com/books?id=caF_AAAAQBAJ&lpg=PA124&ots=OA9Ttj9CHZ&dq=disc%20TOC%20point%20A2&pg=PA124

		public int FirstRecordedTrackNumber;
		public int LastRecordedTrackNumber;
		public int LeadoutLBA;

		/// <summary>
		/// TODO - this hasnt been set for CCD, has it?
		/// </summary>
		public DiscTOCRaw.SessionFormat Session1Format;

		/// <summary>
		/// Appends the new entries to the provided list
		/// </summary>
		public void Run(List<RawTOCEntry> entries)
		{
			SubchannelQ sq = new SubchannelQ();

			//first recorded track number:
			sq.q_index.DecimalValue = 0xA0;
			sq.ap_min.DecimalValue = FirstRecordedTrackNumber;
			switch(Session1Format)
			{
				case DiscTOCRaw.SessionFormat.Type00_CDROM_CDDA: sq.ap_sec.DecimalValue = 0x00; break;
				case DiscTOCRaw.SessionFormat.Type10_CDI: sq.ap_sec.DecimalValue = 0x10; break;
				case DiscTOCRaw.SessionFormat.Type20_CDXA: sq.ap_sec.DecimalValue = 0x20; break;
				default: throw new InvalidOperationException("Invalid Session1Format");
			}
			sq.ap_frame.DecimalValue = 0;

			entries.Add(new RawTOCEntry { QData = sq });

			//last recorded track number:
			sq.q_index.DecimalValue = 0xA1;
			sq.ap_min.DecimalValue = LastRecordedTrackNumber;
			sq.ap_sec.DecimalValue = 0;
			sq.ap_frame.DecimalValue = 0;

			entries.Add(new RawTOCEntry { QData = sq });

			//leadout:
			sq.q_index.DecimalValue = 0xA2;
			sq.AP_Timestamp = new Timestamp(LeadoutLBA);

			entries.Add(new RawTOCEntry { QData = sq });
		}
	}

	/// <summary>
	/// Main unit of organization for reading data from the disc. Represents one physical disc sector.
	/// </summary>
	public class SectorEntry
	{
		public SectorEntry(ISector sec) { Sector = sec; }

		internal ISectorSynthJob2448 SectorSynth;

		/// <summary>
		/// Access the --whatsitcalled-- normal data for the sector with this
		/// </summary>
		public ISector Sector;

		/// <summary>
		/// Access the subcode data for the sector
		/// </summary>
		public ISubcodeSector SubcodeSector;

		//todo - add a PARAMETER fields to this (a long, maybe) so that the ISector can use them (so that each ISector doesnt have to be constructed also)
		//also then, maybe this could be a struct
	}
}