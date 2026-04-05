namespace BizHawk.Client.Common
{
	public interface ILuaLibraries
	{
		LuaFile CurrentFile { get; }

		/// <remarks>pretty hacky... we don't want a lua script to be able to restart itself by rebooting the core</remarks>
		bool IsRebootingCore { get; set; }

		bool IsUpdateSupressed { get; set; }

		PathEntryCollection PathEntries { get; }

		ApiGroup ProhibitedApis { get; }

		NLuaTableHelper GetTableHelper();

		void Sandbox(LuaFile luaFile, Action callback, Action<string> exceptionCallback = null, ApiGroup prohibitedApis = ApiGroup.NONE);
	}
}