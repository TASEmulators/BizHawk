#nullable enable

using System;

namespace BizHawk.Client.Common
{
	/// <summary>Indicates that a type, method, property, etc. is deprecated, and will be removed in a future BizHawk release in 2020 or 2021.</summary>
	[AttributeUsage(AttributeTargets.All)]
	[LegacyApiHawk]
	public sealed class LegacyApiHawkAttribute : Attribute {}
}
