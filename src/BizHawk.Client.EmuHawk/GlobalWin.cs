using BizHawk.Bizware.BizwareGL;

// ReSharper disable StyleCop.SA1401
namespace BizHawk.Client.EmuHawk
{
	public static class GlobalWin
	{
		public static MainForm MainForm;
		public static ToolManager Tools;

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
		public static Communication.HttpCommunication httpCommunication = null;
		public static Communication.SocketServer socketServer = null;
		public static Communication.MemoryMappedFiles memoryMappedFiles = null;

		/// <summary>
		/// Used to disable secondary throttling (e.g. vsync, audio) for unthrottled modes or when the primary (clock) throttle is taking over (e.g. during fast forward/rewind).
		/// </summary>
		public static bool DisableSecondaryThrottling { get; set; }
	}
}
