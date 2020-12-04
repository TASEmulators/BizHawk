using System;

using BizHawk.Client.Common;

using NLua;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Methods intentionally blank.
	/// </summary>
	public sealed class UnixLuaLibraries : LuaLibraries
	{
		public override string EngineName => null;

		public override void CallLoadStateEvent(string name)
		{
		}
		public override void CallSaveStateEvent(string name)
		{
		}

		public override INamedLuaFunction CreateAndRegisterNamedFunction(LuaFunction function, string theEvent, Action<string> logCallback, LuaFile luaFile, string name = null) => null;

		public override NLuaTableHelper GetTableHelper() => null;

		private static readonly LuaFunctionList EmptyLuaFunList = new LuaFunctionList();
		public override LuaFunctionList RegisteredFunctions => EmptyLuaFunList;

		public override bool RemoveNamedFunctionMatching(Func<INamedLuaFunction, bool> predicate) => false;

		public override void SpawnAndSetFileThread(string pathToLoad, LuaFile lf)
		{
		}
	}
}