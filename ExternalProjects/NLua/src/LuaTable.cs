using System;
using System.Collections;
using System.Collections.Generic;

using NLua.Extensions;
using NLua.Native;

namespace NLua
{
	public class LuaTable : LuaBase, IReadOnlyDictionary<object, object>
	{
		public int Count
			=> Wrapped?.Count ?? default;

		public IEnumerable<object>/*?*/ Keys
			=> Wrapped?.Keys;

		public IEnumerable<object/*?*/> Values
			=> Wrapped?.Values as IReadOnlyCollection<object/*?*/> ?? Array.Empty<object/*?*/>();

		private Dictionary<object, object/*?*/>/*?*/ Wrapped
			=> TryGet(out var lua) ? lua.GetTableDict(this) : null;

		public LuaTable(int reference, Lua interpreter): base(reference, interpreter)
		{
		}

		/// <summary>
		/// Indexer for string fields of the table
		/// </summary>
		public object this[string field]
		{
			get => !TryGet(out var lua) ? null : lua.GetObject(_Reference, field);
			set
			{
				if (!TryGet(out var lua))
				{
					return;
				}

				lua.SetObject(_Reference, field, value);
			}
		}

		/// <summary>
		/// Indexer for numeric fields of the table
		/// </summary>
		public object this[object field]
		{
			get => !TryGet(out var lua) ? null : lua.GetObject(_Reference, field);
			set
			{
				if (!TryGet(out var lua))
				{
					return;
				}

				lua.SetObject(_Reference, field, value);
			}
		}

		public bool ContainsKey(object key)
			=> Wrapped?.ContainsKey(key) ?? false;

		public Dictionary<object, object/*?*/>.Enumerator GetEnumerator()
			=> Wrapped?.GetEnumerator() ?? default;

		IEnumerator<KeyValuePair<object, object>> IEnumerable<KeyValuePair<object, object>>.GetEnumerator()
			=> GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator()
			=> GetEnumerator();

		/// <summary>
		/// Gets an string fields of a table ignoring its metatable,
		/// if it exists
		/// </summary>
		internal object RawGet(string field)
			=> !TryGet(out var lua) ? null : lua.RawGetObject(_Reference, field);

		/// <summary>
		/// Pushes this table into the Lua stack
		/// </summary>
		internal void Push(LuaState luaState)
			=> luaState.GetRef(_Reference);

		public override string ToString()
			=> "table";

		public bool TryGetValue(object key, out object/*?*/ value)
		{
			if (Wrapped is Dictionary<object, object/*?*/> dict) return dict.TryGetValue(key, out value);
			value = default;
			return default;
		}
	}
}
