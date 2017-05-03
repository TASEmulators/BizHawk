using System;
using LuaInterface;

namespace BizHawk.Client.Common
{
	public class NamedLuaFunction
	{
		private readonly LuaFunction _function;

		public NamedLuaFunction(LuaFunction function, string theevent, Action<string> logCallback, Lua lua, string name = null)
		{
			_function = function;
			Name = name ?? "Anonymous";
			Event = theevent;
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
					logCallback(
						"error running function attached by the event " +
						Event +
						"\nError message: " +
						ex.Message);
				}
			};
		}

		public Guid Guid { get; private set; }

		public string Name { get; }

		public Lua Lua { get; }

		public string Event { get; }

		public Action Callback { get; }

		public void Call(string name = null)
		{
			LuaSandbox.Sandbox(Lua, () =>
			{
				_function.Call(name);
			});
		}
	}
}
