using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using NLua;

namespace BizHawk.Client.Common
{
	public static class LuaExtensions
	{
		public static LuaTable EnumerateToLuaTable<T>(this NLuaTableHelper tableHelper, IEnumerable<T> list, int indexFrom = 1)
			=> tableHelper.ListToTable(list as IReadOnlyList<T> ?? list.ToList(), indexFrom);

		public static string Serialize(this LuaTable lti)
		{
			var sorted = lti
				.OrderBy(static item => item.Key switch
				{
					long => 0,
					string => 1,
					double => 2,
					bool => 3,
					_ => 4, // tables, functions, ...
				})
				.ThenBy(static item => item.Key as long?)
				.ThenBy(static item => item.Key as double?)
				.ThenBy(static item => item.Key is not (long or double) ? item.Key.ToString() : null, StringComparer.InvariantCulture);

			var sb = new StringBuilder();
			foreach (var item in sorted)
			{
				Append(sb, item.Key);
				sb.Append(": ");
				Append(sb, item.Value);
				sb.Append('\n');
			}
			return sb.ToString();

			static void Append(StringBuilder sb, object value)
			{
				if (value is string str) sb.Append('"').Append(str).Append('"');
				else sb.AppendFormat(CultureInfo.InvariantCulture, "{0}", value);
			}
		}
	}
}
