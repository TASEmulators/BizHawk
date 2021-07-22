using System;

namespace BizHawk.Client.Common
{
	public enum AllowInput
	{
		None = 0,
		All = 1,
		OnlyController = 2
	}

	[Flags]
	public enum ModifierKey
	{
		/// <summary>The bitmask to extract modifiers from a key value.</summary>
		Modifiers = -65536,
		/// <summary>No key pressed.</summary>
		None = 0,
		/// <summary>The SHIFT modifier key.</summary>
		Shift = 65536,
		/// <summary>The CTRL modifier key.</summary>
		Control = 131072,
		/// <summary>The ALT modifier key.</summary>
		Alt = 262144,
	}
}
