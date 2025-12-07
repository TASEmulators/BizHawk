namespace BizHawk.Client.Common
{
	public interface ILuaLibraries
	{
		LuaFile CurrentFile { get; }

		/// <remarks>pretty hacky... we don't want a lua script to be able to restart itself by rebooting the core</remarks>
		bool IsRebootingCore { get; set; }

		bool IsUpdateSupressed { get; set; }

		/// <remarks>not really sure if this is the right place to put it, multiple different places need this...</remarks>
		bool IsInInputOrMemoryCallback { get; set; }

		PathEntryCollection PathEntries { get; }

		NLuaTableHelper GetTableHelper();
	}
}