using System;

namespace BizHawk.Emulation.Common
{
	public class CoreAttributes : Attribute
	{
		public CoreAttributes(string name)
		{
			CoreName = name;
		}

		public string CoreName { get; private set; }
	}
}
