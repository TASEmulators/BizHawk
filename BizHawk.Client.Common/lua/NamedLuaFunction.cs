using System;
using NLua;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class NamedLuaFunction
	{
		private readonly LuaFunction _function;

		public NamedLuaFunction(LuaFunction function, string theEvent, Action<string> logCallback, Lua lua, string name = null)
		{
			_function = function;
			Name = name ?? "Anonymous";
			Event = theEvent;
			Lua = lua;
			Guid = Guid.NewGuid();

			Callback = delegate
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

			MemCallback = delegate
			{
				Callback();
			};
		}

		public Guid Guid { get; }

		public string Name { get; }

		public Lua Lua { get; }

		public string Event { get; }

		public Action Callback { get; }

		public MemoryCallbackDelegate MemCallback { get; }

		public void Call(string name = null)
		{
			LuaSandbox.Sandbox(Lua, () =>
			{
				_function.Call(name);
			});
		}
	}
}
