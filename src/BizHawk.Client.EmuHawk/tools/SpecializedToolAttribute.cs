using System;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Defines a <see cref="IToolForm"/> as a specialized tool that is for a specific system or core
	/// </summary>
	public class SpecializedToolAttribute : Attribute
	{
		public SpecializedToolAttribute(string displayName)
		{
			DisplayName = displayName;
		}

		public string DisplayName { get; }
	}
}
