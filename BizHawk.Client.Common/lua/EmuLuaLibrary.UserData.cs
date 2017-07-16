using System;
using System.ComponentModel;

using NLua;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	[Description("A library for setting and retrieving dynamic data that will be saved and loaded with savestates")]
	public sealed class UserDataLibrary : LuaLibraryBase
	{
		public UserDataLibrary(Lua lua)
			: base(lua) { }

		public UserDataLibrary(Lua lua, Action<string> logOutputCallback)
			: base(lua, logOutputCallback) { }

		public override string Name => "userdata";

		[LuaMethod("set", "adds or updates the data with the given key with the given value")]
		public void Set(string name, object value)
		{
			if (value != null)
			{
				var t = value.GetType();
				if (!t.IsPrimitive && t != typeof(string))
				{
					throw new InvalidOperationException("Invalid type for userdata");
				}
			}

			Global.UserBag[name] = value;
		}

		[LuaMethod("get", "gets the data with the given key, if the key does not exist it will return nil")]
		public object Get(string key)
		{
			if (Global.UserBag.ContainsKey(key))
			{
				return Global.UserBag[key];
			}

			return null;
		}

		[LuaMethod("clear", "clears all user data")]
		public void Clear()
		{
			Global.UserBag.Clear();
		}

		[LuaMethod("remove", "remove the data with the given key. Returns true if the element is successfully found and removed; otherwise, false.")]
		public bool Remove(string key)
		{
			return Global.UserBag.Remove(key);
		}

		[LuaMethod("containskey", "returns whether or not there is an entry for the given key")]
		public bool ContainsKey(string key)
		{
			return Global.UserBag.ContainsKey(key);
		}
	}
}
