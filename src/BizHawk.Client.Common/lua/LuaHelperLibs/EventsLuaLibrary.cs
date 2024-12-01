using System.Linq;
using System.ComponentModel;

using NLua;
using BizHawk.Emulation.Common;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Local
namespace BizHawk.Client.Common
{
	[Description("A library for registering lua functions to emulator events.\n All events support multiple registered methods.\nAll registered event methods can be named and return a Guid when registered")]
	public sealed class EventsLuaLibrary : LuaLibraryBase
	{
		[OptionalService]
		private IInputPollable InputPollableCore { get; set; }

		[OptionalService]
		private IDebuggable DebuggableCore { get; set; }

		[RequiredService]
		private IEmulator Emulator { get; set; }

		[OptionalService]
		private IMemoryDomains Domains { get; set; }

		public EventsLuaLibrary(ILuaLibraries luaLibsImpl, ApiContainer apiContainer, Action<string> logOutputCallback)
			: base(luaLibsImpl, apiContainer, logOutputCallback) {}

		public override string Name => "event";

		private void LogMemoryCallbacksNotImplemented(bool isWildcard)
			=> Log($"{Emulator.Attributes().CoreName} does not implement {(isWildcard ? "wildcard " : string.Empty)}memory callbacks");

		private void LogMemoryExecuteCallbacksNotImplemented(bool isWildcard)
			=> Log($"{Emulator.Attributes().CoreName} does not implement {(isWildcard ? "wildcard " : string.Empty)}memory execute callbacks");

		private void LogScopeNotAvailable(string scope)
		{
			Log($"{scope} is not an available scope for {Emulator.Attributes().CoreName}");
		}

		[LuaMethod("can_use_callback_params", "Returns whether EmuHawk will pass arguments to callbacks. The current version passes arguments to \"memory\" callbacks (RAM/ROM/bus R/W), so this function will return true for that input. (It returns false for any other input.) This tells you whether it's necessary to enable workarounds/hacks because a script is running in a version without parameter support.")]
		[LuaMethodExample("local mem_callback = event.can_use_callback_params(\"memory\") and mem_callback or mem_callback_pre_29;")]
		public bool CanUseCallbackParams(string subset = null)
			=> subset is "memory";

		[LuaMethodExample("local steveonf = event.onframeend(\r\n\tfunction()\r\n\t\tconsole.log( \"Calls the given lua function at the end of each frame, after all emulation and drawing has completed. Note: this is the default behavior of lua scripts\" );\r\n\tend\r\n\t, \"Frame name\" );")]
		[LuaMethod("onframeend", "Calls the given lua function at the end of each frame, after all emulation and drawing has completed. Note: this is the default behavior of lua scripts")]
		public string OnFrameEnd(LuaFunction luaf, string name = null)
			=> _luaLibsImpl.CreateAndRegisterNamedFunction(luaf, NamedLuaFunction.EVENT_TYPE_POSTFRAME, LogOutputCallback, CurrentFile, name)
				.Guid.ToString();

		[LuaMethodExample("local steveonf = event.onframestart(\r\n\tfunction()\r\n\t\tconsole.log( \"Calls the given lua function at the beginning of each frame before any emulation and drawing occurs\" );\r\n\tend\r\n\t, \"Frame name\" );")]
		[LuaMethod("onframestart", "Calls the given lua function at the beginning of each frame before any emulation and drawing occurs")]
		public string OnFrameStart(LuaFunction luaf, string name = null)
			=> _luaLibsImpl.CreateAndRegisterNamedFunction(luaf, NamedLuaFunction.EVENT_TYPE_PREFRAME, LogOutputCallback, CurrentFile, name)
				.Guid.ToString();

		[LuaMethodExample("local steveoni = event.oninputpoll(\r\n\tfunction()\r\n\t\tconsole.log( \"Calls the given lua function after each time the emulator core polls for input\" );\r\n\tend\r\n\t, \"Frame name\" );")]
		[LuaMethod("oninputpoll", "Calls the given lua function after each time the emulator core polls for input")]
		public string OnInputPoll(LuaFunction luaf, string name = null)
		{
			var nlf = _luaLibsImpl.CreateAndRegisterNamedFunction(luaf, NamedLuaFunction.EVENT_TYPE_INPUTPOLL, LogOutputCallback, CurrentFile, name);
			//TODO should we bother registering the function if the service isn't supported? none of the other events work this way --yoshi

			if (InputPollableCore != null)
			{
				try
				{
					InputPollableCore.InputCallbacks.Add(nlf.InputCallback);
					return nlf.Guid.ToString();
				}
				catch (NotImplementedException)
				{
					LogNotImplemented();
					return Guid.Empty.ToString();
				}
			}

			LogNotImplemented();
			return Guid.Empty.ToString();
		}

		private void LogNotImplemented()
		{
			Log($"Error: {Emulator.Attributes().CoreName} does not yet implement input polling callbacks");
		}

		[LuaMethodExample("local steveonl = event.onloadstate(\r\n\tfunction()\r\n\tconsole.log( \"Fires after a state is loaded. Receives a lua function name, and registers it to the event immediately following a successful savestate event\" );\r\nend\", \"Frame name\" );")]
		[LuaMethod("onloadstate", "Fires after a state is loaded. Your callback can have 1 parameter, which will be the name of the loaded state.")]
		public string OnLoadState(LuaFunction luaf, string name = null)
			=> _luaLibsImpl.CreateAndRegisterNamedFunction(luaf, NamedLuaFunction.EVENT_TYPE_LOADSTATE, LogOutputCallback, CurrentFile, name)
				.Guid.ToString();

		[LuaDeprecatedMethod]
		[LuaMethod("onmemoryexecute", "Fires immediately before the given address is executed by the core. Your callback can have 3 parameters {{(addr, val, flags)}}. {{val}} is the value to be executed (or {{0}} always, if this feature is only partially implemented).")]
		public string OnMemoryExecute(
			LuaFunction luaf,
			uint address,
			string name = null,
			string scope = null)
		{
//			Log("Deprecated function event.onmemoryexecute() used, replace the call with event.on_bus_exec().");
			return OnBusExec(luaf, address, name: name, scope: scope);
		}

		[LuaMethodExample("local exec_cb_id = event.on_bus_exec(\r\n\tfunction(addr, val, flags)\r\n\t\tconsole.log( \"Fires immediately before the given address is executed by the core. {{val}} is the value to be executed (or {{0}} always, if this feature is only partially implemented).\" );\r\n\tend\r\n\t, 0x200, \"Frame name\", \"System Bus\" );")]
		[LuaMethod("on_bus_exec", "Fires immediately before the given address is executed by the core. Your callback can have 3 parameters {{(addr, val, flags)}}. {{val}} is the value to be executed (or {{0}} always, if this feature is only partially implemented).")]
		public string OnBusExec(
			LuaFunction luaf,
			uint address,
			string name = null,
			string scope = null)
		{
			try
			{
				if (DebuggableCore is not null
					&& DebuggableCore.MemoryCallbacksAvailable()
					&& DebuggableCore.MemoryCallbacks.ExecuteCallbacksAvailable)
				{
					if (!HasScope(scope))
					{
						LogScopeNotAvailable(scope);
						return Guid.Empty.ToString();
					}

					var nlf = _luaLibsImpl.CreateAndRegisterNamedFunction(luaf, NamedLuaFunction.EVENT_TYPE_MEMEXEC, LogOutputCallback, CurrentFile, name);
					DebuggableCore.MemoryCallbacks.Add(
						new MemoryCallback(ProcessScope(scope), MemoryCallbackType.Execute, "Lua Hook", nlf.MemCallback, address, null));
					return nlf.Guid.ToString();
				}
			}
			catch (NotImplementedException)
			{
				LogMemoryExecuteCallbacksNotImplemented(isWildcard: false);
				return Guid.Empty.ToString();
			}

			LogMemoryExecuteCallbacksNotImplemented(isWildcard: false);
			return Guid.Empty.ToString();
		}

		[LuaDeprecatedMethod]
		[LuaMethod("onmemoryexecuteany", "Fires immediately before every instruction executed (in the specified scope) by the core (CPU-intensive). Your callback can have 3 parameters {{(addr, val, flags)}}. {{val}} is the value to be executed (or {{0}} always, if this feature is only partially implemented).")]
		public string OnMemoryExecuteAny(
			LuaFunction luaf,
			string name = null,
			string scope = null)
		{
//			Log("Deprecated function event.onmemoryexecuteany(...) used, replace the call with event.on_bus_exec_any(...).");
			return OnBusExecAny(luaf, name: name, scope: scope);
		}

		[LuaMethodExample("local exec_cb_id = event.on_bus_exec_any(\r\n\tfunction(addr, val, flags)\r\n\t\tconsole.log( \"Fires immediately before every instruction executed (in the specified scope) by the core (CPU-intensive). {{val}} is the value to be executed (or {{0}} always, if this feature is only partially implemented).\" );\r\n\tend\r\n\t, \"Frame name\", \"System Bus\" );")]
		[LuaMethod("on_bus_exec_any", "Fires immediately before every instruction executed (in the specified scope) by the core (CPU-intensive). Your callback can have 3 parameters {{(addr, val, flags)}}. {{val}} is the value to be executed (or {{0}} always, if this feature is only partially implemented).")]
		public string OnBusExecAny(
			LuaFunction luaf,
			string name = null,
			string scope = null)
		{
			try
			{
				if (DebuggableCore?.MemoryCallbacksAvailable() == true
					&& DebuggableCore.MemoryCallbacks.ExecuteCallbacksAvailable)
				{
					if (!HasScope(scope))
					{
						LogScopeNotAvailable(scope);
						return Guid.Empty.ToString();
					}

					var nlf = _luaLibsImpl.CreateAndRegisterNamedFunction(luaf, NamedLuaFunction.EVENT_TYPE_MEMEXECANY, LogOutputCallback, CurrentFile, name);
					DebuggableCore.MemoryCallbacks.Add(new MemoryCallback(
						ProcessScope(scope),
						MemoryCallbackType.Execute,
						"Lua Hook",
						nlf.MemCallback,
						null,
						null
					));
					return nlf.Guid.ToString();
				}
				// fall through
			}
			catch (NotImplementedException)
			{
				// fall through
			}
			LogMemoryExecuteCallbacksNotImplemented(isWildcard: true);
			return Guid.Empty.ToString();
		}

		[LuaDeprecatedMethod]
		[LuaMethod("onmemoryread", "Fires immediately before the given address is read by the core. Your callback can have 3 parameters {{(addr, val, flags)}}. {{val}} is the value read. If no address is given, it will fire on every memory read.")]
		public string OnMemoryRead(
			LuaFunction luaf,
			uint? address = null,
			string name = null,
			string scope = null)
		{
//			Log("Deprecated function event.onmemoryread(...) used, replace the call with event.on_bus_read(...).");
			return OnBusRead(luaf, address, name: name, scope: scope);
		}

		[LuaMethodExample("local exec_cb_id = event.on_bus_read(\r\n\tfunction(addr, val, flags)\r\n\t\tconsole.log( \"Fires immediately before the given address is read by the core. {{val}} is the value read. If no address is given, it will fire on every memory read.\" );\r\n\tend\r\n\t, 0x200, \"Frame name\" );")]
		[LuaMethod("on_bus_read", "Fires immediately before the given address is read by the core. Your callback can have 3 parameters {{(addr, val, flags)}}. {{val}} is the value read. If no address is given, it will fire on every memory read.")]
		public string OnBusRead(
			LuaFunction luaf,
			uint? address = null,
			string name = null,
			string scope = null)
		{
			try
			{
				if (DebuggableCore?.MemoryCallbacksAvailable() == true)
				{
					if (!HasScope(scope))
					{
						LogScopeNotAvailable(scope);
						return Guid.Empty.ToString();
					}

					var nlf = _luaLibsImpl.CreateAndRegisterNamedFunction(luaf, NamedLuaFunction.EVENT_TYPE_MEMREAD, LogOutputCallback, CurrentFile, name);
					DebuggableCore.MemoryCallbacks.Add(
						new MemoryCallback(ProcessScope(scope), MemoryCallbackType.Read, "Lua Hook", nlf.MemCallback, address, null));
					return nlf.Guid.ToString();
				}
			}
			catch (NotImplementedException)
			{
				LogMemoryCallbacksNotImplemented(isWildcard: address is null);
				return Guid.Empty.ToString();
			}

			LogMemoryCallbacksNotImplemented(isWildcard: address is null);
			return Guid.Empty.ToString();
		}

		[LuaDeprecatedMethod]
		[LuaMethod("onmemorywrite", "Fires immediately before the given address is written by the core. Your callback can have 3 parameters {{(addr, val, flags)}}. {{val}} is the value to be written (or {{0}} always, if this feature is only partially implemented). If no address is given, it will fire on every memory write.")]
		public string OnMemoryWrite(
			LuaFunction luaf,
			uint? address = null,
			string name = null,
			string scope = null)
		{
//			Log("Deprecated function event.onmemorywrite(...) used, replace the call with event.on_bus_write(...).");
			return OnBusWrite(luaf, address, name: name, scope: scope);
		}

		[LuaMethodExample("local exec_cb_id = event.on_bus_write(\r\n\tfunction(addr, val, flags)\r\n\t\tconsole.log( \"Fires immediately before the given address is written by the core. {{val}} is the value to be written (or {{0}} always, if this feature is only partially implemented). If no address is given, it will fire on every memory write.\" );\r\n\tend\r\n\t, 0x200, \"Frame name\" );")]
		[LuaMethod("on_bus_write", "Fires immediately before the given address is written by the core. Your callback can have 3 parameters {{(addr, val, flags)}}. {{val}} is the value to be written (or {{0}} always, if this feature is only partially implemented). If no address is given, it will fire on every memory write.")]
		public string OnBusWrite(
			LuaFunction luaf,
			uint? address = null,
			string name = null,
			string scope = null)
		{
			try
			{
				if (DebuggableCore?.MemoryCallbacksAvailable() == true)
				{
					if (!HasScope(scope))
					{
						LogScopeNotAvailable(scope);
						return Guid.Empty.ToString();
					}

					var nlf = _luaLibsImpl.CreateAndRegisterNamedFunction(luaf, NamedLuaFunction.EVENT_TYPE_MEMWRITE, LogOutputCallback, CurrentFile, name);
					DebuggableCore.MemoryCallbacks.Add(
						new MemoryCallback(ProcessScope(scope), MemoryCallbackType.Write, "Lua Hook", nlf.MemCallback, address, null));
					return nlf.Guid.ToString();
				}
			}
			catch (NotImplementedException)
			{
				LogMemoryCallbacksNotImplemented(isWildcard: address is null);
				return Guid.Empty.ToString();
			}

			LogMemoryCallbacksNotImplemented(isWildcard: address is null);
			return Guid.Empty.ToString();
		}

		[LuaMethodExample("local steveons = event.onsavestate(\r\n\tfunction()\r\n\t\tconsole.log( \"Fires after a state is saved\" );\r\n\tend\r\n\t, \"Frame name\" );")]
		[LuaMethod("onsavestate", "Fires after a state is saved. Your callback can have 1 parameter, which will be the name of the saved state.")]
		public string OnSaveState(LuaFunction luaf, string name = null)
			=> _luaLibsImpl.CreateAndRegisterNamedFunction(luaf, NamedLuaFunction.EVENT_TYPE_SAVESTATE, LogOutputCallback, CurrentFile, name)
				.Guid.ToString();

		[LuaMethodExample("local steveone = event.onexit(\r\n\tfunction()\r\n\t\tconsole.log( \"Fires after the calling script has stopped\" );\r\n\tend\r\n\t, \"Frame name\" );")]
		[LuaMethod("onexit", "Fires after the calling script has stopped")]
		public string OnExit(LuaFunction luaf, string name = null)
			=> _luaLibsImpl.CreateAndRegisterNamedFunction(luaf, NamedLuaFunction.EVENT_TYPE_ENGINESTOP, LogOutputCallback, CurrentFile, name)
				.Guid.ToString();

		[LuaMethodExample("local closeGuid = event.onconsoleclose(\r\n\tfunction()\r\n\t\tconsole.log( \"Fires when the emulator console closes\" );\r\n\tend\r\n\t, \"Frame name\" );")]
		[LuaMethod("onconsoleclose", "Fires when the emulator console closes")]
		public string OnConsoleClose(LuaFunction luaf, string name = null)
			=> _luaLibsImpl.CreateAndRegisterNamedFunction(luaf, NamedLuaFunction.EVENT_TYPE_CONSOLECLOSE, LogOutputCallback, CurrentFile, name)
				.Guid.ToString();

		[LuaMethodExample("if ( event.unregisterbyid( \"4d1810b7 - 0d28 - 4acb - 9d8b - d87721641551\" ) ) then\r\n\tconsole.log( \"Removes the registered function that matches the guid.If a function is found and remove the function will return true.If unable to find a match, the function will return false.\" );\r\nend;")]
		[LuaMethod("unregisterbyid", "Removes the registered function that matches the guid. If a function is found and remove the function will return true. If unable to find a match, the function will return false.")]
		public bool UnregisterById(string guid)
			=> _luaLibsImpl.RemoveNamedFunctionMatching(nlf => nlf.Guid.ToString() == guid);

		[LuaMethodExample("if ( event.unregisterbyname( \"Function name\" ) ) then\r\n\tconsole.log( \"Removes the first registered function that matches Name.If a function is found and remove the function will return true.If unable to find a match, the function will return false.\" );\r\nend;")]
		[LuaMethod("unregisterbyname", "Removes the first registered function that matches Name. If a function is found and remove the function will return true. If unable to find a match, the function will return false.")]
		public bool UnregisterByName(string name)
			=> _luaLibsImpl.RemoveNamedFunctionMatching(nlf => nlf.Name == name);

		[LuaMethodExample("local scopes = event.availableScopes();")]
		[LuaMethod("availableScopes", "Lists the available scopes that can be specified for on_bus_* events")]
		public LuaTable AvailableScopes()
		{
			return DebuggableCore?.MemoryCallbacksAvailable() == true
				? _th.ListToTable(DebuggableCore.MemoryCallbacks.AvailableScopes, indexFrom: 0)
				: _th.CreateTable();
		}

		private string ProcessScope(string scope)
		{
			if (string.IsNullOrWhiteSpace(scope))
			{
				if (Domains != null && Domains.HasSystemBus)
				{
					scope = Domains.SystemBus.Name;
				}
				else
				{
					scope = DebuggableCore.MemoryCallbacks.AvailableScopes[0];
				}
			}

			return scope;
		}

		private bool HasScope(string scope)
		{
			return string.IsNullOrWhiteSpace(scope) || DebuggableCore.MemoryCallbacks.AvailableScopes.Contains(scope);
		}
	}
}
