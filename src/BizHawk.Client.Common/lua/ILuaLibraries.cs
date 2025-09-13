using NLua;

namespace BizHawk.Client.Common
{
	public interface ILuaLibraries
	{
		LuaDocumentation Docs { get; }

		string EngineName { get; }

		/// <remarks>pretty hacky... we don't want a lua script to be able to restart itself by rebooting the core</remarks>
		bool IsRebootingCore { get; set; }

		bool IsUpdateSupressed { get; set; }

		/// <remarks>not really sure if this is the right place to put it, multiple different places need this...</remarks>
		bool IsInInputOrMemoryCallback { get; set; }

		PathEntryCollection PathEntries { get; }

		INamedLuaFunction CreateAndRegisterNamedFunction(
			LuaFunction function,
			string theEvent,
			Action<string> logCallback,
			LuaFile luaFile,
			string name = null);

		NLuaTableHelper GetTableHelper();

		bool RemoveNamedFunctionMatching(Func<INamedLuaFunction, bool> predicate);
	}
}