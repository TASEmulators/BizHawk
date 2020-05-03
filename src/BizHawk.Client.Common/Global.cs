using System.Collections.Generic;
using BizHawk.Emulation.Common;

// ReSharper disable StyleCop.SA1401
namespace BizHawk.Client.Common
{
	public static class Global
	{
		public static IEmulator Emulator { get; set; }
		public static Config Config { get; set; }
		public static GameInfo Game { get; set; }
		public static CheatCollection CheatList { get; set; } = new CheatCollection();
		public static FirmwareManager FirmwareManager { get; set; }

		public static IMovieSession MovieSession { get; set; }

		/// <summary>
		/// Used to disable secondary throttling (e.g. vsync, audio) for unthrottled modes or when the primary (clock) throttle is taking over (e.g. during fast forward/rewind).
		/// </summary>
		public static bool DisableSecondaryThrottling { get; set; }

		/// <summary>
		/// The maximum number of milliseconds the sound output buffer can go below full before causing a noticeable sound interruption.
		/// </summary>
		public static int SoundMaxBufferDeficitMs { get; set; }

		public static InputManager InputManager { get; } = new InputManager();

		public static Dictionary<string, object> UserBag { get; set; } = new Dictionary<string, object>();
	}
}
