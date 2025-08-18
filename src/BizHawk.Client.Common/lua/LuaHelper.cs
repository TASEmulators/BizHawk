using System.Collections.Generic;
using System.Linq;

using NLua;

namespace BizHawk.Client.Common
{
	public static class LuaExtensions
	{
		public static LuaTable EnumerateToLuaTable<T>(this NLuaTableHelper tableHelper, IEnumerable<T> list, int indexFrom = 1)
			=> tableHelper.ListToTable(list as IReadOnlyList<T> ?? list.ToList(), indexFrom);
	}
}
