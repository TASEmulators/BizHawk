using System;

using NLua;

namespace BizHawk.Client.Common
{
	public interface IPlatformLuaLibEnv
	{
		LuaDocumentation Docs { get; }

		string EngineName { get; }

		/// <remarks>pretty hacky... we don't want a lua script to be able to restart itself by rebooting the core</remarks>
		bool IsRebootingCore { get; set; }

		bool IsUpdateSupressed { get; set; }

		LuaFunctionList RegisteredFunctions { get; }

		LuaFileList ScriptList { get; }

		void CallLoadStateEvent(string name);

		void CallSaveStateEvent(string name);

		INamedLuaFunction CreateAndRegisterNamedFunction(LuaFunction function, string theEvent, Action<string> logCallback, LuaFile luaFile, string name = null);

		NLuaTableHelper GetTableHelper();

		bool RemoveNamedFunctionMatching(Func<INamedLuaFunction, bool> predicate);

		void SpawnAndSetFileThread(string pathToLoad, LuaFile lf);
	}
}