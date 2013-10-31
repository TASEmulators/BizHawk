using System;
using LuaInterface;

namespace BizHawk.Client.Common
{
	public class NamedLuaFunction
	{
		private readonly LuaFunction _function;
		private readonly string _name;
		private readonly string _event;

		public NamedLuaFunction(LuaFunction function, string theevent, string name = null)
		{
			_function = function;
			_name = name ?? "Anonymous";
			_event = theevent;
			GUID = Guid.NewGuid();
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
	}
}
