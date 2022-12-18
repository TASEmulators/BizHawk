using System;

namespace NLua.Event
{
	/// <summary>
	/// Event args for hook callback event
	/// </summary>
	public class DebugHookEventArgs : EventArgs
	{
		public DebugHookEventArgs(LuaDebug luaDebug)
		{
			LuaDebug = luaDebug;
		}

		/// <summary>
		/// Lua Debug Information
		/// </summary>
		public LuaDebug LuaDebug { get; }
	}
}
