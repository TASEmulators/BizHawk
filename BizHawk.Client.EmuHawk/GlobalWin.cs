using BizHawk.Client.Common;
using BizHawk.Bizware.BizwareGL;

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

		public static GLManager.ContextRef CR_GL;

		/// <summary>
		/// The IGL_TK to be used for specifically opengl operations (accessing textures from opengl-based cores)
		/// </summary>
		public static Bizware.BizwareGL.Drivers.OpenTK.IGL_TK IGL_GL;

		public static Sound Sound;
		public static OSDManager OSD = new OSDManager();
		public static DisplayManager DisplayManager;
		public static GLManager GLManager;

		public static int ExitCode;
	}
}
