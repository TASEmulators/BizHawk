using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using NLua;

namespace BizHawk.Client.Common
{
	public static class LuaExtensions
	{
		public static LuaTable ToLuaTable<T>(this IDictionary<string, T> dictionary, Lua lua)
		{
			var table = lua.NewTable();

			foreach (var kvp in dictionary)
			{
				table[kvp.Key] = kvp.Value;
			}

			return table;
		}

		public static LuaTable TableFromObject(this Lua lua, object obj)
		{
			var table = lua.NewTable();

			var type = obj.GetType();

			var methods = type.GetMethods();
			foreach (var method in methods)
			{
				if (method.IsPublic)
				{
					string luaName = ""; // Empty will default to the actual method name;

					var luaMethodAttr = (LuaMethodAttribute)method.GetCustomAttributes(typeof(LuaMethodAttribute)).FirstOrDefault();
					if (luaMethodAttr != null)
					{
						luaName = luaMethodAttr.Name;
					}

					table[method.Name] = lua.RegisterFunction(luaName, obj, method);
				}
			}

			return table;
		}
	}
}
