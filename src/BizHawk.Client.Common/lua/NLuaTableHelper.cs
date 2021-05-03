using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;

using NLua;

namespace BizHawk.Client.Common
{
	public sealed class NLuaTableHelper
	{
		private readonly Action<string> _logCallback;

		private readonly Lua _lua;

		public NLuaTableHelper(Lua lua, Action<string> logCallback)
		{
			_logCallback = logCallback;
			_lua = lua;
		}

		public LuaTable CreateTable() => _lua.NewTable();

		public LuaTable DictToTable<T>(IDictionary<string, T> dictionary)
		{
			var table = _lua.NewTable();

			foreach (var kvp in dictionary)
			{
				table[kvp.Key] = kvp.Value;
			}

			return table;
		}

		public IEnumerable<(TKey Key, TValue Value)> EnumerateEntries<TKey, TValue>(LuaTable table)
			=> table.Keys.Cast<TKey>().Select(k => (k, (TValue) table[k]));

		public IEnumerable<T> EnumerateValues<T>(LuaTable table) => table.Values.Cast<T>();

		public LuaTable ListToTable<T>(IList<T> list, int indexFrom = 0)
		{
			var table = _lua.NewTable();
			for (int i = 0, l = list.Count; i != l; i++) table[indexFrom + i] = list[i];
			return table;
		}

		public LuaTable ObjectToTable(object obj)
		{
			var table = _lua.NewTable();
			foreach (var method in obj.GetType().GetMethods())
			{
				if (!method.IsPublic) continue;
				var foundAttrs = method.GetCustomAttributes(typeof(LuaMethodAttribute), false);
				table[method.Name] = _lua.RegisterFunction(
					foundAttrs.Length == 0 ? string.Empty : ((LuaMethodAttribute) foundAttrs[0]).Name, // empty string will default to the actual method name
					obj,
					method
				);
			}
			return table;
		}

		public Color ParseColor(object o) => ParseColor(o, safe: false, _logCallback) ?? throw new ArgumentException("failed to parse Color", nameof(o));

		public Color? SafeParseColor(object o) => ParseColor(o, safe: true, _logCallback);

		private static Color? ParseColor(object o, bool safe, Action<string> logCallback)
		{
			switch (o)
			{
				case null:
					return null;
				case Color c:
					return c;
				case double d:
					return ParseColor((int) (long) d, safe, logCallback);
				case int i:
					return Color.FromArgb(i);
				case string s:
					if (s[0] == '#' && s.Length == 9) return ParseColor(int.Parse(s.Substring(1), NumberStyles.HexNumber), safe, logCallback);
					var fromName = Color.FromName(s);
					if (fromName.IsNamedColor) return fromName;
					if (safe) logCallback($"ParseColor: not a known color name (\"{s}\")");
					return null;
				default:
					if (safe) logCallback("ParseColor: coercing object/table to string");
					return ParseColor(o.ToString(), safe, logCallback);
			}
		}
	}
}
