using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	internal class LuaLibraries : LuaLibrariesBase
	{
		private MainForm _mainForm;

		public LuaLibraries(
			LuaFileList scriptList,
			LuaFunctionList registeredFuncList,
			MainForm mainForm,
			DisplayManagerBase displayManager,
			InputManager inputManager,
			Config config,
			IGameInfo game)
			: base(scriptList, registeredFuncList, mainForm, displayManager, inputManager, config, game)
		{
			_mainForm = mainForm;
			RegisterLuaLibraries(ReflectionCache.Types);
		}

		protected override void HandleSpecialLuaLibraryProperties(LuaLibraryBase library)
		{
			base.HandleSpecialLuaLibraryProperties(library);

			// TODO: make EmuHawk libraries have a base class with common properties such as this
			// and inject them here
			if (library is ConsoleLuaLibrary consoleLib)
			{
				consoleLib.Tools = _mainForm.Tools;
				_logToLuaConsoleCallback = consoleLib.Log;
			}
			else if (library is FormsLuaLibrary formsLib)
			{
				formsLib.MainForm = _mainForm;
			}
			else if (library is TAStudioLuaLibrary tastudioLib)
			{
				tastudioLib.Tools = _mainForm.Tools;
			}
			else if (library is GuiLuaLibrary guiLib) // GuiLuaLibrary isn't in EmuHawk, but LuaCanvas is.
			{
				// emu lib may be null now, depending on order of ReflectionCache.Types, but definitely won't be null when this is called
				guiLib.CreateLuaCanvasCallback = (width, height, x, y) =>
				{
					var canvas = new LuaCanvas(EmulationLuaLibrary, width, height, x, y, GetTableHelper(), LogToLuaConsole);
					canvas.Show();
					return GetTableHelper().ObjectToTable(canvas);
				};

				EnumerateLuaFunctions(nameof(LuaCanvas), typeof(LuaCanvas), null); // add LuaCanvas to Lua function reference table
			}

		}
	}
}
