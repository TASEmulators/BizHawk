using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Client.EmuHawk
{
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class ToolAttribute : Attribute
	{
		[CLSCompliant(false)]
		public ToolAttribute(bool released, string[] supportedSystems, string[] unsupportedCores)
		{
			Released = released;
			SupportedSystems = supportedSystems ?? Enumerable.Empty<string>();
			UnsupportedCores = unsupportedCores ?? Enumerable.Empty<string>();
		}

		[CLSCompliant(false)]
		public ToolAttribute(bool released, string[] supportedSystems) : this(released, supportedSystems, null) {}

		public ToolAttribute(bool released) : this(released, null, null) {}

		public bool Released { get; }

		public IEnumerable<string> SupportedSystems { get; }

		public IEnumerable<string> UnsupportedCores { get; }
	}
}
