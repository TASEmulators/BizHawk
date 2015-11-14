using System;
using System.Collections.Generic;

namespace BizHawk.Client.EmuHawk
{
	[AttributeUsage(AttributeTargets.Class)]
	public class ToolAttributes : Attribute
	{
		public ToolAttributes(bool released, string[] supportedSystems)
		{
			Released = released;
			SupportedSystems = supportedSystems;
		}

		public bool Released { get; private set; }

		public IEnumerable<string> SupportedSystems { get; private set; }
	}
}
