using BizHawk.Bizware.BizwareGL;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using System.Collections.Generic;

// ReSharper disable StyleCop.SA1401
namespace BizHawk.Client.EmuHawk
{
	public static class GlobalWin
	{
		public static ToolManager Tools;

		public static IEmulator Emulator { get; set; }

		/// <summary>
		/// the IGL to be used for rendering
		/// </summary>
		public static IGL GL;

		/// <summary>
		/// The IGL_TK to be used for specifically opengl operations (accessing textures from opengl-based cores)
		/// </summary>
		public static IGL_TK IGL_GL;

		public static Sound Sound;
		public static readonly OSDManager OSD = new OSDManager();
		public static DisplayManager DisplayManager;
		public static GLManager GLManager;

		public static int ExitCode;

		/// <summary>
		/// Used to disable secondary throttling (e.g. vsync, audio) for unthrottled modes or when the primary (clock) throttle is taking over (e.g. during fast forward/rewind).
		/// </summary>
		public static bool DisableSecondaryThrottling { get; set; }

		public static Dictionary<string, object> UserBag { get; set; } = new Dictionary<string, object>();

		public static Config Config { get; set; }
		public static FirmwareManager FirmwareManager { get; set; }
		public static GameInfo Game { get; set; }
		public static IMovieSession MovieSession { get; set; }
		public static InputManager InputManager { get; } = new InputManager();

		public static EmuClientApi ClientApi { get; set; }
	}
}
