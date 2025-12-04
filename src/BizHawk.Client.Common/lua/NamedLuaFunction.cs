using BizHawk.Emulation.Common;
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

		public Action/*?*/ OnRemove { get; set; } = null;

		public NamedLuaFunction(LuaFunction function, string theEvent, Action<string> logCallback, LuaFile luaFile,
			ILuaLibraries luaLibraries, string name = null)
		{
			_function = function;
			_luaImp = luaLibraries;
			_exceptionCallback = logCallback;
			Name = name ?? "Anonymous";
			Event = theEvent;
			LuaFile = luaFile;

			Guid = Guid.NewGuid();

			InputCallback = () =>
			{
				luaLibraries.IsInInputOrMemoryCallback = true;
				Call(Array.Empty<object>());
				luaLibraries.IsInInputOrMemoryCallback = false;
			};
			MemCallback = (addr, val, flags) =>
			{
				luaLibraries.IsInInputOrMemoryCallback = true;
				uint? ret =  Call([ addr, val, flags ]) is [ long n ] ? unchecked((uint) n) : null;
				luaLibraries.IsInInputOrMemoryCallback = false;
				return ret;
			};
			RandomCallback = pr_class =>
			{
				luaLibraries.IsInInputOrMemoryCallback = true;
				Call([ pr_class ]);
				luaLibraries.IsInInputOrMemoryCallback = false;
			};
			LineCallback = (line, thing) =>
			{
				luaLibraries.IsInInputOrMemoryCallback = true;
				Call([ line, thing ]);
				luaLibraries.IsInInputOrMemoryCallback = false;
			};
		}

		public Guid Guid { get; }

		public string GuidStr
			=> Guid.ToString("D");

		public string Name { get; }

		private LuaFile LuaFile { get; }

		public string Event { get; }

		public Action InputCallback { get; }

		public MemoryCallbackDelegate MemCallback { get; }

		public Action<int> RandomCallback { get; }

		public Action<long, long> LineCallback { get; }

		public void Call(string name = null)
		{
			_luaImp.Sandbox(LuaFile, () => _  = _function.Call(name), (s) =>
				_exceptionCallback($"error running function attached by the event {Event}\nError message: {s}"));
		}

		public object[] Call(params object[] args)
		{
			object[] ret = null;
			_luaImp.Sandbox(LuaFile, () => ret = _function.Call(args), (s) =>
				_exceptionCallback($"error running function attached by the event {Event}\nError message: {s}"));
			return ret;
		}
	}
}
