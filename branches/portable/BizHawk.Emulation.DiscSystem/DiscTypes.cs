using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace BizHawk.Emulation.DiscSystem
{
	/// <summary>
	/// Represents a TOC entry discovered in the Q subchannel data of the lead-in track.
	/// It isn't clear whether we need anything other than the SubchannelQ data, so I abstracted this in case we need it.
	/// </summary>
	public class RawTOCEntry
	{
		public SubchannelQ QData;
	}

	/// <summary>
	/// Main unit of organization for reading data from the disc. Represents one physical disc sector.
	/// </summary>
	public class SectorEntry
	{
		public SectorEntry(ISector sec) { Sector = sec; }

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