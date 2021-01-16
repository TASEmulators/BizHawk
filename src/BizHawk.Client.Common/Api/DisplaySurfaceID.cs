#nullable enable

using System;

namespace BizHawk.Client.Common
{
	public enum DisplaySurfaceID : int
	{
		EmuCore = 0,
		Client = 1,
	}

	/// <remarks>should probably centralise these enum extensions and not-extensions somewhere... --yoshi</remarks>
	public static class DisplaySurfaceIDParser
	{
		public static DisplaySurfaceID? Parse(string? str) => str?.ToLowerInvariant() switch
		{
			null => null, // this makes it easy to cascade the "remembered" value
			"client" => DisplaySurfaceID.Client,
			"emu" => DisplaySurfaceID.EmuCore,
			"emucore" => DisplaySurfaceID.EmuCore,
			"native" => DisplaySurfaceID.Client,
			_ => throw new ArgumentException(message: $"{str} is not the name of a display surface", paramName: nameof(str))
		};

		public static string GetName(this DisplaySurfaceID surfaceID) => surfaceID switch
		{
			DisplaySurfaceID.EmuCore => "emucore",
			DisplaySurfaceID.Client => "client",
			_ => throw new ArgumentException(message: "not a valid enum member", paramName: nameof(surfaceID))
		};
	}
}
