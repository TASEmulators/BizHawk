using System;
using System.Collections;

using NLua.Extensions;
using NLua.Native;

namespace NLua
{
	public class LuaTable : LuaBase
	{
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

		public IDictionaryEnumerator GetEnumerator()
		{
			if (!TryGet(out var lua))
			{
				return null;
			}

			return lua.GetTableDict(this).GetEnumerator();
		}

		public ICollection Keys => !TryGet(out var lua) ? null : lua.GetTableDict(this).Keys;

		public ICollection Values
		{
			get
			{
				if (!TryGet(out var lua))
				{
					return Array.Empty<object>();
				}

				return lua.GetTableDict(this).Values;
			}
		}

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
	}
}
