using System.Buffers;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;

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

		public LuaTable DictToTable<T>(IReadOnlyDictionary<string, T> dictionary)
		{
			var table = CreateTable();
			foreach (var (k, v) in dictionary) table[k] = v;
			return table;
		}

		public ArraySegment<T> EnumerateValues<T>(LuaTable table)
		{
			var n = table.Count;
			if (n is 0) return [ ];
			var values = new T[n];
			var seen = ArrayPool<bool>.Shared.Rent(n);
			seen.AsSpan(start: 0, length: n).Fill(false);
			int cutoff;
			try
			{
				foreach (var (k, v) in table) if (k is long i && 1 <= i && i <= n)
				{
					values[i - 1] = (T) v;
					seen[i - 1] = true;
				}
				cutoff = Array.IndexOf(seen, value: false);
			}
			finally
			{
				ArrayPool<bool>.Shared.Return(seen);
			}
			if (cutoff < 0) return new(values); // all present
			if (cutoff is 0)
			{
				_logCallback("no numeric keys");
				return [ ];
			}
			if (cutoff < n) _logCallback($"ignoring {n - cutoff} entries");
			return new(values, offset: 0, count: cutoff);
		}

		public LuaTable ListToTable<T>(IReadOnlyList<T> list, int indexFrom = 1)
		{
			var table = CreateTable();
			for (int i = 0, l = list.Count; i != l; i++) table[indexFrom + i] = list[i];
			return table;
		}

		public LuaTable MemoryBlockToTable(IReadOnlyList<byte> bytes, long startAddr)
		{
			var length = bytes.Count;
			var table = CreateTable();
			var iArray = 0;
			var iDict = startAddr;
			while (iArray < length) table[iDict++] = bytes[iArray++];
			return table;
		}

		public LuaTable ObjectToTable(object obj)
		{
			var table = CreateTable();
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
				case long l:
					return Color.FromArgb((int)l);
				case string s:
					if (s[0] is '#' && (s.Length is 7 or 9))
					{
						var i1 = uint.Parse(s.Substring(1), NumberStyles.HexNumber);
						if (s.Length is 7) i1 |= 0xFF000000U;
						return ParseColor(unchecked((int) i1), safe, logCallback);
					}
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
