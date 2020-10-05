#nullable enable

using System;

namespace BizHawk.Client.Common
{
	[Flags]
	public enum ClientInputFocus
	{
		None = 0,
		Mouse = 1,
		Keyboard = 2,
		Pad = 4
	}
}
