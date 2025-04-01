using NLua;

using BizHawk.Emulation.Common;

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

		public NamedLuaFunction(LuaFunction function, string theEvent, Action<string> logCallback, LuaFile luaFile,
			Func<LuaThread> createThreadCallback, ILuaLibraries luaLibraries, string name = null)
		{
			_function = function;
			Name = name ?? "Anonymous";
			Event = theEvent;
			CreateThreadCallback = createThreadCallback;
			LuaLibraries = luaLibraries;

			// When would a file be null?
			// When a script is loaded with a callback, but no infinite loop so it closes
			// Then that callback proceeds to register more callbacks
			// In these situations, we will generate a thread for this new callback on the fly here
			// Scenarios like this suggest that a thread being managed by a LuaFile is a bad idea,
			// and we should refactor
			if (luaFile == null)
			{
				DetachFromScript();
			}
			else
			{
				LuaFile = luaFile;
			}

			Guid = Guid.NewGuid();

			Callback = args =>
			{
				try
				{
					object[] LuaFunctionReturn = _function.Call(args);
					if(LuaFunctionReturn?.Length != 0) // If the value returned is not specified in the callback, the array will not have any elements.
					{
						return Convert.ToUInt32(LuaFunctionReturn[0]);
					}
					return null;
				}
				catch (Exception ex)
				{
					logCallback($"error running function attached by the event {Event}\nError message: {ex.Message}");
				}
				return null;
			};
			InputCallback = () =>
			{
				LuaLibraries.IsInInputOrMemoryCallback = true;
				try
				{
					Callback(Array.Empty<object>());
				}
				finally
				{
					LuaLibraries.IsInInputOrMemoryCallback = false;
				}
			};
			MemCallback = (addr, val, flags) =>
			{
				LuaLibraries.IsInInputOrMemoryCallback = true;
				try
				{
					uint? CallbackReturn = Callback(new object[] { addr, val, flags });
					if (CallbackReturn is null)
					{
						return val;
					}
					return (uint) CallbackReturn;
				}
				finally
				{
					LuaLibraries.IsInInputOrMemoryCallback = false;
				}
			};
		}

		public void DetachFromScript()
		{
			var thread = CreateThreadCallback();

			// Current dir will have to do for now, but this will inevitably not be desired
			// Users will expect it to be the same directly as the thread that spawned this callback
			// But how do we know what that directory was?
			LuaSandbox.CreateSandbox(thread, ".");
			LuaFile = new LuaFile(".") { Thread = thread };
		}

		public Guid Guid { get; }

		public string Name { get; }

		public LuaFile LuaFile { get; private set; }

		/// <summary>
		/// HACK
		/// </summary>
		private ILuaLibraries LuaLibraries { get; }

		private Func<LuaThread> CreateThreadCallback { get; }

		public string Event { get; }

		private Func<object[],uint?> Callback { get; }

		public Action InputCallback { get; }

		public MemoryCallbackDelegate MemCallback { get; }

		public void Call(string name = null)
		{
			LuaSandbox.Sandbox(LuaFile.Thread, () =>
			{
				_function.Call(name);
			});
		}
	}
}
