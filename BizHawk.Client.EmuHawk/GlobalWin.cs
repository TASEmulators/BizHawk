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
		public static Bizware.BizwareGL.Drivers.OpenTK.IGL_TK IGL_GL;

		public static Sound Sound;
		public static readonly OSDManager OSD = new OSDManager();
		public static DisplayManager DisplayManager;
		public static GLManager GLManager;

		public static int ExitCode;
		#if !WINDOWS
		/// <summary>
		/// This flag is designed specifically for non-windows platforms. 
		/// In WinForms you can check to see if the application is active by checking if the ActiveForm is null.
		/// In Mono, the active form does not become null if the application goes to the background, it remains
		/// the frontmost window of the application. We need this flag to prevent input from being read in the background.
		/// Flag is set by the native wrapper on OS X, Linux will need a wrapper if it wants to use this.
		/// </summary>
		public static bool IsApplicationActive = true;
		#endif
	}
}
