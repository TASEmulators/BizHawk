using System;

namespace BizHawk.Emulation.Common
{
	public class CoreAttributes : Attribute
	{
		public CoreAttributes(string name, string author, bool isPorted = false, bool isReleased = false)
		{
			CoreName = name;
			Author = author;
			Ported = isPorted;
			Released = isReleased;
		}

		public string CoreName { get; private set; }
		public string Author { get; private set; }
		public bool Ported { get; private set; }
		public bool Released { get; private set; }
	}
}
