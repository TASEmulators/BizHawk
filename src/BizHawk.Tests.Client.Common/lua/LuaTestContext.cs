using System.Collections.Generic;
using System.IO;
using System.Linq;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Tests.Client.Common.Movie;

namespace BizHawk.Tests.Client.Common.lua
{
	internal class LuaTestContext
	{
		private static readonly string BASE_SCRIPT_PATH = Path.Combine(Environment.CurrentDirectory, "lua/scripts");

		private LuaLibraries lua;

		public List<string> loggedMessages = new();

		private FakeEmulator emulator = new();

		public LuaTestContext()
		{
			Action<string> print = loggedMessages.Add;
			FakeMainFormForApi mainApi = new();
			Config config = new();

			ApiContainer apiContainer = ApiManager.RestartLua(
				emulator.ServiceProvider,
				print,
				mainApi,
				new SimpleGDIPDisplayManager(config, emulator),
				null!,
				null!,
				null!,
				config,
				emulator,
				new GameInfo(),
				null!);


			lua = new(
				new LuaFileList([ ], () => { }),
				emulator.ServiceProvider,
				mainApi,
				config,
				print,
				null!,
				apiContainer
			);
		}

		public void AddScript(string path, bool enable)
		{
			string absolutePath = Path.GetFullPath(Path.Combine(BASE_SCRIPT_PATH, path));
			LuaFile luaFile = new(absolutePath, () => { });
			lua.ScriptList.Add(luaFile);
			if (enable)
			{
				luaFile.Thread = lua.SpawnCoroutine(luaFile.Path);
				LuaSandbox.CreateSandbox(luaFile.Thread, Path.GetDirectoryName(luaFile.Path));
				luaFile.State = LuaFile.RunState.Running;
			}
		}

		public void StopScript(int id)
		{
			lua.ScriptList[id].Stop();
		}

		public void RunYielding()
		{
			lua.ResumeScripts(false);
		}

		public void RunFrameWaiting()
		{
			Controller c = new(emulator.ControllerDefinition);
			emulator.FrameAdvance(c, true);
			lua.CallFrameAfterEvent();
			lua.ResumeScripts(true);
		}

		public void AssertLogMatches(params string[] messages)
		{
			Assert.AreEqual(messages.Length, loggedMessages.Count);
			for (int i = 0; i < messages.Length; i++)
			{
				Assert.AreEqual(messages[i], loggedMessages[i]);
			}
		}

		public LuaFile.RunState GetScriptState(int id)
		{
			return lua.ScriptList[id].State;
		}

		public List<NamedLuaFunction> FunctionsRegisteredToScript(int id)
		{
			return lua.ScriptList[id].Functions.ToList();
		}
	}
}
