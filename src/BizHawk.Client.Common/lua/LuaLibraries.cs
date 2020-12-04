using System;

using NLua;

namespace BizHawk.Client.Common
{
	public abstract class LuaLibraries
	{
		public readonly LuaDocumentation Docs = new LuaDocumentation();

		public abstract string EngineName { get; }

		public abstract LuaFunctionList RegisteredFunctions { get; }
		public readonly LuaFileList ScriptList = new LuaFileList();

		public bool IsRebootingCore { get; set; } // pretty hacky.. we don't want a lua script to be able to restart itself by rebooting the core

		public bool IsUpdateSupressed { get; set; }

		public abstract void CallLoadStateEvent(string name);
		public abstract void CallSaveStateEvent(string name);

		public abstract INamedLuaFunction CreateAndRegisterNamedFunction(LuaFunction function, string theEvent, Action<string> logCallback, LuaFile luaFile, string name = null);

		public abstract NLuaTableHelper GetTableHelper();

		public abstract bool RemoveNamedFunctionMatching(Func<INamedLuaFunction, bool> predicate);

		public abstract void SpawnAndSetFileThread(string pathToLoad, LuaFile lf);
	}
}