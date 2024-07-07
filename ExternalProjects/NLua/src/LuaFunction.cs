using System;

using NLua.Native;

namespace NLua
{
	public class LuaFunction : LuaBase
	{
		internal readonly LuaNativeFunction function;

		public LuaFunction(int reference, Lua interpreter):base(reference, interpreter)
		{
			function = null;
		}

		public LuaFunction(LuaNativeFunction nativeFunction, Lua interpreter):base (0, interpreter)
		{
			function = nativeFunction;
		}

		/// <summary>
		/// Calls the function casting return values to the types
		/// in returnTypes
		/// </summary>
		internal object[] Call(object[] args, Type[] returnTypes)
			=> !TryGet(out var lua) ? null : lua.CallFunction(this, args, returnTypes);

		/// <summary>
		/// Calls the function and returns its return values inside
		/// an array
		/// </summary>
		public object[] Call(params object[] args)
			=> !TryGet(out var lua) ? null : lua.CallFunction(this, args);

		/// <summary>
		/// Pushes the function into the Lua stack
		/// </summary>
		internal void Push(LuaState luaState)
		{
			if (!TryGet(out var lua))
			{
				return;
			}

			if (_Reference != 0)
			{
				luaState.RawGetInteger(LuaRegistry.Index, _Reference);
			}
			else
			{
				lua.PushCSFunction(function);
			}
		}

		public override string ToString()
			=> "function";

		public override bool Equals(object o)
		{
			if (o is not LuaFunction l)
			{
				return false;
			}

			if (!TryGet(out var lua))
			{
				return false;
			}

			if (_Reference != 0 && l._Reference != 0)
			{
				return lua.CompareRef(l._Reference, _Reference);
			}

			return function == l.function;
		}

		public override int GetHashCode()
			=> _Reference != 0 ? _Reference : function.GetHashCode();
	}
}
