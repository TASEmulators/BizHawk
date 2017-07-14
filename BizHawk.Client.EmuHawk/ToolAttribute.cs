using System;
using System.Collections.Generic;

namespace BizHawk.Client.EmuHawk
{
	[AttributeUsage(AttributeTargets.Class)]
	public class ToolAttribute : Attribute
	{
		public ToolAttribute(bool released, string[] supportedSystems)
		{
			Released = released;
			SupportedSystems = supportedSystems;
		}

		public bool Released { get; private set; }

		public IEnumerable<string> SupportedSystems { get; private set; }
	}
}
