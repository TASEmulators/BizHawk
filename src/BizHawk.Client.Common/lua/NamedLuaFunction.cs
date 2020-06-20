using System;
using NLua;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class NamedLuaFunction
	{
		private readonly LuaFunction _function;

		public NamedLuaFunction(LuaFunction function, string theEvent, Action<string> logCallback, LuaFile luaFile, string name = null)
		{
			_function = function;
			Name = name ?? "Anonymous";
			Event = theEvent;
			LuaFile = luaFile;
			Guid = Guid.NewGuid();

			Callback = () =>
			{
				try
				{
					_function.Call();
				}
				catch (Exception ex)
				{
					logCallback($"error running function attached by the event {Event}\nError message: {ex.Message}");
				}
			};

			MemCallback = (address, value, flags) => Callback();
		}

		public Guid Guid { get; }

		public string Name { get; }

		public LuaFile LuaFile { get; }

		public string Event { get; }

		public Action Callback { get; }

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
