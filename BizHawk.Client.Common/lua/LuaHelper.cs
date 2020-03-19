using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using NLua;

namespace BizHawk.Client.Common
{
	public static class LuaExtensions
	{
		/// <remarks><c>-0x1FFFFFFFFFFFFF..0x1FFFFFFFFFFFFF</c></remarks>
		private static readonly Range<double> PrecisionLimits = (-9007199254740991.0).RangeTo(9007199254740991.0);

		/// <remarks>
		/// Lua numbers are always double-length floats, so integers whose magnitude is at least 2^53 may not fit in the 53-bit mantissa (the sign is stored separately).
		/// These extremely large values aren't that useful, so we'll just assume they're erroneous and give the script author an error.
		/// </remarks>
		/// <exception cref="ArithmeticException"><paramref name="d"/> &ge; 2^53 or <paramref name="d"/> &le; -2^53</exception>
		public static long AsInteger(this double d) => PrecisionLimits.Contains(d) ? (long) d : throw new ArithmeticException("integer value exceeds the precision of Lua's integer-as-double");

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
