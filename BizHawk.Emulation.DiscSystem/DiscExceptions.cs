using System;
using System.Collections.Generic;

using BizHawk.Common.BufferExtensions;

//some old junk

namespace BizHawk.Emulation.DiscSystem
{
	[Serializable]
	public class DiscReferenceException : Exception
	{
		public DiscReferenceException(string fname, Exception inner)
			: base(string.Format("A disc attempted to reference a file which could not be accessed or loaded: {0}", fname), inner)
		{
		}
		public DiscReferenceException(string fname, string extrainfo)
			: base(string.Format("A disc attempted to reference a file which could not be accessed or loaded:\n\n{0}\n\n{1}", fname, extrainfo))
		{
		}
	}

	
}