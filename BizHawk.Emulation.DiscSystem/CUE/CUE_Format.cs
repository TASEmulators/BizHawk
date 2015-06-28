using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Generic;

//http://digitalx.org/cue-sheet/index.html "all cue sheet information is a straight 1:1 copy from the cdrwin helpfile"

namespace BizHawk.Emulation.DiscSystem
{
	public partial class CUE_Format2
	{
		/// <summary>
		/// The CueFileResolver to be used by this instance
		/// </summary>
		public CueFileResolver Resolver;
	}
}