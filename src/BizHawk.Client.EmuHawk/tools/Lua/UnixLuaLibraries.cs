using System;

using BizHawk.Client.Common;

using NLua;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Methods intentionally blank.
	/// </summary>
	public sealed class UnixLuaLibraries : IPlatformLuaLibEnv
	{
		public LuaDocumentation Docs { get; } = new LuaDocumentation();

		public string EngineName => null;

		public bool IsRebootingCore { get; set; }

		public bool IsUpdateSupressed { get; set; }

		public LuaFunctionList RegisteredFunctions { get; }

		public PathEntryCollection PathEntries { get; }

		public LuaFileList ScriptList { get; }

		public UnixLuaLibraries(LuaFileList scriptList, LuaFunctionList registeredFuncList, PathEntryCollection pathEntries)
		{
			PathEntries = pathEntries;
			RegisteredFunctions = registeredFuncList;
			ScriptList = scriptList;
		}

		public void CallLoadStateEvent(string name) {}

		public void CallSaveStateEvent(string name) {}

		public INamedLuaFunction CreateAndRegisterNamedFunction(
			LuaFunction function,
			string theEvent,
			Action<string> logCallback,
			LuaFile luaFile,
			[LuaArbitraryStringParam] string name = null)
				=> null;

		public NLuaTableHelper GetTableHelper() => null;

		public bool RemoveNamedFunctionMatching(Func<INamedLuaFunction, bool> predicate) => false;

		public void SpawnAndSetFileThread(string pathToLoad, LuaFile lf) {}
	}
}