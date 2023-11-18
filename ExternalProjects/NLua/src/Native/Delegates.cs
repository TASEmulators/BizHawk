using System.Runtime.InteropServices;
using System.Security;

using lua_State = System.IntPtr;

namespace NLua.Native
{
	/// <summary>
	/// Type for C# callbacks
	/// In order to communicate properly with Lua, a C function must use the following protocol, which defines the way parameters and results are passed: a C function receives its arguments from Lua in its stack in direct order (the first argument is pushed first). So, when the function starts, lua_gettop(L) returns the number of arguments received by the function. The first argument (if any) is at index 1 and its last argument is at index lua_gettop(L). To return values to Lua, a C function just pushes them onto the stack, in direct order (the first result is pushed first), and returns the number of results. Any other value in the stack below the results will be properly discarded by Lua. Like a Lua function, a C function called by Lua can also return many results. 
	/// </summary>
	/// <param name="luaState"></param>
	/// <returns></returns>
	[SuppressUnmanagedCodeSecurity]
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate int LuaNativeFunction(lua_State luaState);
}
