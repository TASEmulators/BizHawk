#pragma warning disable
#nullable enable // for when this file is embedded

using System;

namespace BizHawk.SrcGen.CLSCompliance
{
	[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Struct | AttributeTargets.Class)]
	public sealed class AutoderiveCLSComplianceAttribute : Attribute {}
}
#pragma warning restore
