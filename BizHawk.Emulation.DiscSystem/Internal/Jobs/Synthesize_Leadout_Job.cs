using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Generic;

//TODO - generate correct Q subchannel CRC

namespace BizHawk.Emulation.DiscSystem
{

	/// <summary>
	/// generates lead-out sectors according to very crude approximations
	/// TODO - this isnt being used right now
	/// </summary>
	class Synthesize_LeadoutJob
	{
		public int Length;
		public Disc Disc;

		public void Run()
		{
			//TODO: encode_mode2_form2_sector

			var leadoutTs = Disc.TOC.LeadoutLBA;
			var lastTrackTOCItem = Disc.TOC.TOCItems[Disc.TOC.LastRecordedTrackNumber]; //NOTE: in case LastRecordedTrackNumber is al ie, this will malfunction

			//leadout flags.. let's set them the same as the last track.
			//THIS IS NOT EXACTLY THE SAME WAY MEDNAFEN DOES IT
			EControlQ leadoutFlags = lastTrackTOCItem.Control;

			//TODO - needs to be encoded as a certain mode (mode 2 form 2 for psx... i guess...)

			for (int i = 0; i < Length; i++)
			{
				//var se = new SectorEntry(sz);
				//Disc.Sectors.Add(se);
				SubchannelQ sq = new SubchannelQ();

				int track_relative_msf = i;
				sq.min = BCD2.FromDecimal(new Timestamp(track_relative_msf).MIN);
				sq.sec = BCD2.FromDecimal(new Timestamp(track_relative_msf).SEC);
				sq.frame = BCD2.FromDecimal(new Timestamp(track_relative_msf).FRAC);

				int absolute_msf = i + leadoutTs;
				sq.ap_min = BCD2.FromDecimal(new Timestamp(absolute_msf + 150).MIN);
				sq.ap_sec = BCD2.FromDecimal(new Timestamp(absolute_msf + 150).SEC);
				sq.ap_frame = BCD2.FromDecimal(new Timestamp(absolute_msf + 150).FRAC);

				sq.q_tno.DecimalValue = 0xAA; //special value for leadout
				sq.q_index.DecimalValue = 1;

				byte ADR = 1;
				sq.SetStatus(ADR, leadoutFlags);

				//TODO - actually stash the subQ
			}
		}
	}
}