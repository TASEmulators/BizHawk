using System.Collections.Generic;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Tests
{
	internal class TestLuaLibraries : LuaLibrariesBase
	{
		public TestLuaLibraries(IMainFormForApi mainForm, DisplayManagerBase displayManager, Config config, IGameInfo game)
			: base(new LuaFileList(
				new List<LuaFile>(), () => { }),
				  new LuaFunctionList(() => { }),
				  mainForm,
				  displayManager,
				  new InputManager(),
				  config,
				  game
			)
		{ }
	}
}
