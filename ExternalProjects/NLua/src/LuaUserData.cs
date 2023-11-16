using NLua.Extensions;
using NLua.Native;

namespace NLua
{
	public class LuaUserData : LuaBase
	{
		public LuaUserData(int reference, Lua interpreter)
			: base(reference, interpreter)
		{
		}

		/// <summary>
		/// Indexer for string fields of the userdata
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
		/// Indexer for numeric fields of the userdata
		/// </summary>
		public object this[object field]
		{
			get => !TryGet(out var lua) ? null : lua.GetObject(_Reference, field);
			set
			{
				if (!TryGet(out var lua))
					return;

				lua.SetObject(_Reference, field, value);
			}
		}

		/// <summary>
		/// Calls the userdata and returns its return values inside
		/// an array
		/// </summary>
		public object[] Call(params object[] args)
			=> !TryGet(out var lua) ? null : lua.CallFunction(this, args);

		/// <summary>
		/// Pushes this userdata into the Lua stack
		/// </summary>
		internal void Push(LuaState luaState)
			=> luaState.GetRef(_Reference);

		public override string ToString()
			=> "userdata";
	}
}
