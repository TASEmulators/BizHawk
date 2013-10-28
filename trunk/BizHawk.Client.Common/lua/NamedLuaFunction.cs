using System;
using System.Linq;
using System.Text;

using LuaInterface;

namespace BizHawk.Client.Common
{
	public class NamedLuaFunction
	{
		private LuaFunction _function;
		private string _name;
		private string _event;

		public NamedLuaFunction(LuaFunction function, string theevent, string name = null)
		{
			_function = function;
			_name = name ?? "Anonymous Function";
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
