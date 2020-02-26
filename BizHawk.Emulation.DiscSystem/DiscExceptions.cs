using System;

//some old junk

namespace BizHawk.Emulation.DiscSystem
{
	[Serializable]
	public class DiscReferenceException : Exception
	{
		public DiscReferenceException(string fname, Exception inner)
			: base($"A disc attempted to reference a file which could not be accessed or loaded: {fname}", inner)
		{
		}
		public DiscReferenceException(string fname, string extrainfo)
			: base($"A disc attempted to reference a file which could not be accessed or loaded:\n\n{fname}\n\n{extrainfo}")
		{
		}
	}

	
}