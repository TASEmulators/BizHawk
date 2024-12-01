using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Client.EmuHawk
{
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class ToolAttribute : Attribute
	{
		public ToolAttribute(bool released, string[] supportedSystems, string[] unsupportedCores)
		{
			Released = released;
			SupportedSystems = supportedSystems ?? Enumerable.Empty<string>();
			UnsupportedCores = unsupportedCores ?? Enumerable.Empty<string>();
		}

		public ToolAttribute(bool released, string[] supportedSystems) : this(released, supportedSystems, null) {}

		public bool Released { get; }

		public IEnumerable<string> SupportedSystems { get; }

		public IEnumerable<string> UnsupportedCores { get; }
	}
}
