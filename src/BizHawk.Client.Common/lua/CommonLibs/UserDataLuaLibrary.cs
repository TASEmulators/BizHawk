using System;
using System.ComponentModel;

// ReSharper disable UnusedMember.Global
namespace BizHawk.Client.Common
{
	[Description("A library for setting and retrieving dynamic data that will be saved and loaded with savestates")]
	public sealed class UserDataLuaLibrary : LuaLibraryBase
	{
		public UserDataLuaLibrary(IPlatformLuaLibEnv luaLibsImpl, ApiContainer apiContainer, Action<string> logOutputCallback)
			: base(luaLibsImpl, apiContainer, logOutputCallback) {}

		public override string Name => "userdata";

		[LuaMethodExample("userdata.set(\"Unique key\", \"Current key data\");")]
		[LuaMethod("set", "adds or updates the data with the given key with the given value")]
		public void Set([LuaArbitraryStringParam] string name, [LuaArbitraryStringParam] object value)
			=> APIs.UserData.Set(FixString(name), value is string s ? FixString(s) : value);

		[LuaMethodExample("local obuseget = userdata.get( \"Unique key\" );")]
		[LuaMethod("get", "gets the data with the given key, if the key does not exist it will return nil")]
		[return: LuaArbitraryStringParam]
		public object Get([LuaArbitraryStringParam] string key)
		{
			var o = APIs.UserData.Get(FixString(key));
			return o is string s ? UnFixString(s) : o;
		}

		[LuaMethodExample("userdata.clear( );")]
		[LuaMethod("clear", "clears all user data")]
		public void Clear()
			=> APIs.UserData.Clear();

		[LuaMethodExample("if ( userdata.remove( \"Unique key\" ) ) then\r\n\tconsole.log( \"remove the data with the given key.Returns true if the element is successfully found and removed; otherwise, false.\" );\r\nend;")]
		[LuaMethod("remove", "remove the data with the given key. Returns true if the element is successfully found and removed; otherwise, false.")]
		public bool Remove([LuaArbitraryStringParam] string key)
			=> APIs.UserData.Remove(FixString(key));

		[LuaMethodExample("if ( userdata.containskey( \"Unique key\" ) ) then\r\n\tconsole.log( \"returns whether or not there is an entry for the given key\" );\r\nend;")]
		[LuaMethod("containskey", "returns whether or not there is an entry for the given key")]
		public bool ContainsKey([LuaArbitraryStringParam] string key)
			=> APIs.UserData.ContainsKey(FixString(key));
	}
}
