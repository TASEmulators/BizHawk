using System;
using System.Collections.Generic;
using LuaInterface;

namespace BizHawk.Client.Common
{
	public class NamedLuaFunction
	{
		private readonly LuaFunction _function;
		private readonly string _name;
		private readonly string _event;
		private readonly Action _action;
		public NamedLuaFunction(LuaFunction function, string theevent, Action<string> logCallback, string name = null)
		{
			_function = function;
			_name = name ?? "Anonymous";
			_event = theevent;
			GUID = Guid.NewGuid();

			_action = new Action(delegate
			{
				try
				{
					_function.Call();
				}
				catch (SystemException ex)
				{
					logCallback(
						"error running function attached by the event " +
						_event +
						"\nError message: " +
						ex.Message
					);
				}

			});
		}

		public Guid GUID { get; private set; }

		public void Call(string name = null)
		{
			_function.Call(name);
		}

		public string Name
		{
			get { return _name; }
		}

		public string Event
		{
			get { return _event; }
		}

		public Action Callback
		{
			get { return _action; }
		}
	}
}
