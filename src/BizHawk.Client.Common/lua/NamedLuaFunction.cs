using NLua;

namespace BizHawk.Client.Common
{
	public sealed class NamedLuaFunction : INamedLuaFunction
	{
		public const string EVENT_TYPE_CONSOLECLOSE = "OnConsoleClose";

		public const string EVENT_TYPE_ENGINESTOP = "OnExit";

		public const string EVENT_TYPE_INPUTPOLL = "OnInputPoll";

		public const string EVENT_TYPE_LOADSTATE = "OnSavestateLoad";

		public const string EVENT_TYPE_MEMEXEC = "OnMemoryExecute";

		public const string EVENT_TYPE_MEMEXECANY = "OnMemoryExecuteAny";

		public const string EVENT_TYPE_MEMREAD = "OnMemoryRead";

		public const string EVENT_TYPE_MEMWRITE = "OnMemoryWrite";

		public const string EVENT_TYPE_POSTFRAME = "OnFrameEnd";

		public const string EVENT_TYPE_PREFRAME = "OnFrameStart";

		public const string EVENT_TYPE_SAVESTATE = "OnSavestateSave";

		private readonly LuaFunction _function;

		private readonly ILuaLibraries _luaImp;

		private readonly Action<string> _exceptionCallback;


		private readonly ApiGroup _prohibitedApis;

		public Action/*?*/ OnRemove { get; set; } = null;

		public NamedLuaFunction(LuaFunction function, string theEvent, Action<string> logCallback, LuaFile luaFile,
			ILuaLibraries luaLibraries, ApiGroup prohibitedApis, string name = null)
		{
			_function = function;
			_luaImp = luaLibraries;
			_exceptionCallback = logCallback;
			_prohibitedApis = prohibitedApis;
			Name = name ?? "Anonymous";
			Event = theEvent;
			LuaFile = luaFile;

#pragma warning disable RS0030 // this is to ensure no collisions
			Guid = Guid.NewGuid();
#pragma warning restore RS0030
		}

		public Guid Guid { get; }

		public string GuidStr
			=> Guid.ToString("D");

		public string Name { get; }

		private LuaFile LuaFile { get; }

		public string Event { get; }

		public object[] Call(params object[] args)
		{
			object[] ret = null;
			_luaImp.Sandbox(
				luaFile: LuaFile,
				callback: () => ret = _function.Call(args),
				exceptionCallback: (s) => _exceptionCallback($"error running function attached by the event {Event}\nError message: {s}"),
				prohibitedApis: _prohibitedApis);

			return ret;
		}
	}
}
