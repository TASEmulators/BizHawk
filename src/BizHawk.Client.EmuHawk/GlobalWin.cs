using BizHawk.Bizware.BizwareGL;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

// ReSharper disable StyleCop.SA1401
namespace BizHawk.Client.EmuHawk
{
	public static class GlobalWin
	{
		public static MainForm _mainForm { get; set; }

		public static IEmulator Emulator => _mainForm.Emulator;

		/// <summary>
		/// the IGL to be used for rendering
		/// </summary>
		public static IGL GL;

		/// <summary>
		/// The IGL_TK to be used for specifically opengl operations (accessing textures from opengl-based cores)
		/// </summary>
		public static IGL_TK IGL_GL;

		public static Sound Sound;

		public static Config Config { get; set; }

		public static GameInfo Game => _mainForm.Game;

		public static IMovieSession MovieSession => _mainForm.MovieSession;

		public static InputManager InputManager { get; } = new InputManager();
	}
}
