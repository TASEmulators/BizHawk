using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LuaInterface;
using System.Reflection;

namespace BizHawk.Client.Common
{
	public static class LuaHelper
	{
		public static LuaTable ToLuaTable(Lua lua, object obj)
		{
			var table = lua.NewTable();

			var type = obj.GetType();

			var methods = type.GetMethods();
			foreach (var method in methods)
			{
				if (method.IsPublic)
				{
					table[method.Name] = lua.RegisterFunction("", obj, method);
				}
			}

			return table;
		}
	}
}
