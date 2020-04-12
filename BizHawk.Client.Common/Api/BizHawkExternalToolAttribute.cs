using System;

using BizHawk.Client.Common;

namespace BizHawk.Client.Common
{
	/// <remarks>This class needs to be in the assembly or old tools will throw on load instead of being recognised as old.</remarks>
	[AttributeUsage(AttributeTargets.Assembly)]
	[Obsolete("last used in 2.4, use [ExternalTool] instead")]
	public sealed class BizHawkExternalToolAttribute : Attribute
	{
		public BizHawkExternalToolAttribute(string name, string description, string iconResourceName) {}
		public BizHawkExternalToolAttribute(string name, string description) {}
		public BizHawkExternalToolAttribute(string name) {}
	}

	/// <inheritdoc cref="BizHawkExternalToolAttribute"/>
	[AttributeUsage(AttributeTargets.Assembly)]
	[Obsolete("last used in 2.4, use [ExternalToolApplicability.*] instead")]
	public sealed class BizHawkExternalToolUsageAttribute : Attribute
	{
		public BizHawkExternalToolUsageAttribute(BizHawkExternalToolUsage usage, CoreSystem system, string gameHash) {}
		public BizHawkExternalToolUsageAttribute(BizHawkExternalToolUsage usage, CoreSystem system) {}
		public BizHawkExternalToolUsageAttribute() {}
	}
}
