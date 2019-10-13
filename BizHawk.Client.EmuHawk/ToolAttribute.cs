using System;
using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Client.EmuHawk
{
	[AttributeUsage(AttributeTargets.Class)]
	public class ToolAttribute : Attribute
	{
		public ToolAttribute(bool released, string[] supportedSystems, string[] unsupportedCores = null)
		{
			Released = released;
			SupportedSystems = supportedSystems ?? Enumerable.Empty<string>();
			UnsupportedCores = unsupportedCores ?? Enumerable.Empty<string>();
		}

		public bool Released { get; }

		public IEnumerable<string> SupportedSystems { get; }

		public IEnumerable<string> UnsupportedCores { get; }
	}
}
