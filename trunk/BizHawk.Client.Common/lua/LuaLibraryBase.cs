using System;
using System.Linq;

using LuaInterface;

namespace BizHawk.Client.Common
{
	public abstract class LuaLibraryBase
	{
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

		public virtual void LuaRegister(Lua lua, ILuaDocumentation docs = null)
		{
			lua.NewTable(Name);

			var luaAttr = typeof(LuaMethodAttributes);

			var methods = GetType()
							.GetMethods()
							.Where(m => m.GetCustomAttributes(luaAttr, false).Any());

			foreach (var method in methods)
			{
				var luaMethodAttr = method.GetCustomAttributes(luaAttr, false).First() as LuaMethodAttributes;
				var luaName = Name + "." + luaMethodAttr.Name;
				lua.RegisterFunction(luaName, this, method);

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
