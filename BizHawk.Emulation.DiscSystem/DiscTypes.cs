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
}