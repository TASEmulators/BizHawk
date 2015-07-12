using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Generic;

//http://digitalx.org/cue-sheet/index.html "all cue sheet information is a straight 1:1 copy from the cdrwin helpfile"

namespace BizHawk.Emulation.DiscSystem.CUE
{
	public class CUE_Context
	{
		/// <summary>
		/// The CueFileResolver to be used by this instance
		/// </summary>
		public CueFileResolver Resolver;

		/// <summary>
		/// The DiscMountPolicy to be applied to this context
		/// </summary>
		public DiscMountPolicy DiscMountPolicy;
	}
}