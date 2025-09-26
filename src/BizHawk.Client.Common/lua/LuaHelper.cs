using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using NLua;

namespace BizHawk.Client.Common
{
	public static class LuaExtensions
	{
		[CLSCompliant(NLuaTableHelper.CLS_LUATABLE)]
		public static LuaTable EnumerateToLuaTable<T>(this NLuaTableHelper tableHelper, IEnumerable<T> list, int indexFrom = 1)
			=> tableHelper.ListToTable(list as IReadOnlyList<T> ?? list.ToList(), indexFrom);

		/// <remarks>Intended for printing tables to the console</remarks>
		public static string PrettyPrintShallow(this LuaTable lti)
		{
			var sorted = lti
				.OrderBy(static item => item.Key switch
				{
					long => 0,
					double => 0,
					string => 1,
					bool => 2,
					_ => 3, // tables, functions, ...
				})
				.ThenBy(static item => item.Key switch
				{
					double d => d,
					long i => i,
					_ => (double?)null,
				})
				.ThenBy(static item => item.Key as long?) // sort large integers that double can't represent
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
