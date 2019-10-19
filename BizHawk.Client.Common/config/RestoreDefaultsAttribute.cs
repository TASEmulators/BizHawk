using System;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// Defines a method to be called when a tool dialog's Restore Defaults method is called
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class RestoreDefaultsAttribute : Attribute
	{
	}
}
