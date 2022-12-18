using System;

namespace NLua
{
	/// <summary>
	/// Lua Hook Event Masks
	/// </summary>
	[Flags]
	public enum LuaHookMask
	{
		/// <summary>
		/// Disabled hook
		/// </summary>
		Disabled = 0,
		/// <summary>
		/// The call hook: is called when the interpreter calls a function. The hook is called just after Lua enters the new function, before the function gets its arguments. 
		/// </summary>
		Call = 1 << LuaHookEvent.Call,
		/// <summary>
		/// The return hook: is called when the interpreter returns from a function. The hook is called just before Lua leaves the function. There is no standard way to access the values to be returned by the function. 
		/// </summary>
		Return = 1 << LuaHookEvent.Return,
		/// <summary>
		/// The line hook: is called when the interpreter is about to start the execution of a new line of code, or when it jumps back in the code (even to the same line). (This event only happens while Lua is executing a Lua function.) 
		/// </summary>
		Line = 1 << LuaHookEvent.Line,
		/// <summary>
		///  The count hook: is called after the interpreter executes every count instructions. (This event only happens while Lua is executing a Lua function.) 
		/// </summary>
		Count = 1 << LuaHookEvent.Count,
	}
}
