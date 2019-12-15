using System.Collections.Generic;
using System.Linq;

using NLua;

namespace BizHawk.Client.Common
{
	public static class LuaExtensions
	{
		public static LuaTable EnumerateToLuaTable<T>(this IEnumerable<T> list, Lua lua) => list.ToList().ToLuaTable(lua);

		public static LuaTable ToLuaTable<T>(this IList<T> list, Lua lua, int indexFrom = 0)
		{
			var table = lua.NewTable();
			var indexAfterLast = indexFrom + list.Count;
			for (var i = indexFrom; i != indexAfterLast; i++)
			{
				table[i] = list[i];
			}
			return table;
		}

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
			foreach (var method in obj.GetType().GetMethods())
			{
				if (!method.IsPublic) continue;
				var foundAttrs = method.GetCustomAttributes(typeof(LuaMethodAttribute), false);
				table[method.Name] = lua.RegisterFunction(
					foundAttrs.Length == 0 ? string.Empty : ((LuaMethodAttribute) foundAttrs[0]).Name, // empty string will default to the actual method name
					obj,
					method
				);
			}
			return table;
		}
	}
}
