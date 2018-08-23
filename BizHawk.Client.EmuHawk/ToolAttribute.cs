using System;
using System.Collections.Generic;

namespace BizHawk.Client.EmuHawk
{
	[AttributeUsage(AttributeTargets.Class)]
	public class ToolAttribute : Attribute
	{
		public ToolAttribute(bool released, string[] supportedSystems, string[] unsupportedCores = null)
		{
			Released = released;
			SupportedSystems = supportedSystems;
            UnsupportedCores = unsupportedCores;
		}

		public bool Released { get; private set; }

		public IEnumerable<string> SupportedSystems { get; private set; }

        public IEnumerable<string> UnsupportedCores { get; private set; }
	}
}
