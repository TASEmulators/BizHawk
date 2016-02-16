using System;
using LuaInterface;

namespace BizHawk.Client.Common
{
	public class NamedLuaFunction
	{
		private readonly LuaFunction _function;
		private readonly string _name;
		private readonly string _event;
		private readonly Action _action;

		public NamedLuaFunction(LuaFunction function, string theevent, Action<string> logCallback, Lua lua, string name = null)
		{
			_function = function;
			_name = name ?? "Anonymous";
			_event = theevent;
			Lua = lua;
			Guid = Guid.NewGuid();

			_action = delegate
			{
				try
				{
					_function.Call();
				}
				catch (Exception ex)
				{
					logCallback(
						"error running function attached by the event " +
						_event +
						"\nError message: " +
						ex.Message);
				}
			};
		}

		public Guid Guid { get; private set; }

		public string Name
		{
			get { return _name; }
		}

		public Lua Lua { get; private set; }

		public string Event
		{
			get { return _event; }
		}

		public Action Callback
		{
			get { return _action; }
		}

		public void Call(string name = null)
		{
			LuaSandbox.Sandbox(Lua, () => {
				_function.Call(name);
			});
		}
	}
}
