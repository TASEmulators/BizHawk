using System;
using System.Linq;

using LuaInterface;

namespace BizHawk.Client.Common
{
	public abstract class LuaLibraryBase
	{
		public LuaLibraryBase(Lua lua)
		{
			Lua = lua;
		}

		public LuaLibraryBase(Lua lua, Action<string> logOutputCallback)
			: this(lua)
		{
			LogOutputCallback = logOutputCallback;
		}

		public abstract string Name { get; }
		public Action<string> LogOutputCallback { get; set; }
		public Lua Lua { get; set; }

		protected void Log(object message)
		{
			if (LogOutputCallback != null)
			{
				LogOutputCallback(message.ToString());
			}
		}

		public virtual void LuaRegister(ILuaDocumentation docs = null)
		{
			Lua.NewTable(Name);

			var luaAttr = typeof(LuaMethodAttributes);

			var methods = GetType()
							.GetMethods()
							.Where(m => m.GetCustomAttributes(luaAttr, false).Any());

			foreach (var method in methods)
			{
				var luaMethodAttr = method.GetCustomAttributes(luaAttr, false).First() as LuaMethodAttributes;
				var luaName = Name + "." + luaMethodAttr.Name;
				Lua.RegisterFunction(luaName, this, method);

				if (docs != null)
				{
					docs.Add(Name, luaMethodAttr.Name, method, luaMethodAttr.Description);
				}
			}
		}

		protected static int LuaInt(object luaArg)
		{
			return (int)(double)luaArg;
		}

		protected static uint LuaUInt(object luaArg)
		{
			return (uint)(double)luaArg;
		}
	}
}
