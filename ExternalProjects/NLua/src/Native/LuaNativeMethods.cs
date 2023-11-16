using System.Runtime.InteropServices;
using System.Security;

using BizHawk.BizInvoke;

using charptr_t = System.IntPtr;
using lua_Alloc = System.IntPtr;
using lua_CFunction = System.IntPtr;
using lua_Integer = System.Int64;
using lua_KContext = System.IntPtr;
using lua_KFunction = System.IntPtr;
using lua_Number = System.Double;
using lua_Reader = System.IntPtr;
using lua_State = System.IntPtr;
using lua_Writer = System.IntPtr;
using size_t = System.UIntPtr;
using voidptr_t = System.IntPtr;

#pragma warning disable SA1121 // Use built-in type alias
#pragma warning disable IDE1006 // Naming rule violation

namespace NLua.Native
{
	[SuppressUnmanagedCodeSecurity]
	public abstract class LuaNativeMethods
	{
		protected const CallingConvention cc = CallingConvention.Cdecl;

		[BizImport(cc)]
		public abstract int lua_absindex(lua_State luaState, int idx);

		[BizImport(cc)]
		public abstract void lua_arith(lua_State luaState, int op);

		[BizImport(cc)]
		public abstract lua_CFunction lua_atpanic(lua_State luaState, lua_CFunction panicf);

		[BizImport(cc)]
		public abstract void lua_callk(lua_State luaState, int nargs, int nresults, lua_KContext ctx, lua_KFunction k);

		[BizImport(cc)]
		public abstract int lua_checkstack(lua_State luaState, int extra);

		[BizImport(cc)]
		public abstract void lua_close(lua_State luaState);

		[BizImport(cc)]
		public abstract int lua_compare(lua_State luaState, int index1, int index2, int op);

		[BizImport(cc)]
		public abstract void lua_concat(lua_State luaState, int n);

		[BizImport(cc)]
		public abstract void lua_copy(lua_State luaState, int fromIndex, int toIndex);

		[BizImport(cc)]
		public abstract void lua_createtable(lua_State luaState, int elements, int records);

		[BizImport(cc)]
		public abstract int lua_dump(lua_State luaState, lua_Writer writer, voidptr_t data, int strip);

		[BizImport(cc)]
		public abstract int lua_error(lua_State luaState);

		[BizImport(cc)]
		public abstract lua_Alloc lua_getallocf(lua_State luaState, ref voidptr_t ud);

		[BizImport(cc)]
		public abstract int lua_getfield(lua_State luaState, int index, string k);

		[BizImport(cc)]
		public abstract int lua_getglobal(lua_State luaState, string name);

		[BizImport(cc)]
		public abstract int lua_geti(lua_State luaState, int index, long i);

		[BizImport(cc)]
		public abstract int lua_getmetatable(lua_State luaState, int index);

		[BizImport(cc)]
		public abstract int lua_gettable(lua_State luaState, int index);

		[BizImport(cc)]
		public abstract int lua_gettop(lua_State luaState);

		[BizImport(cc)]
		public abstract charptr_t lua_getupvalue(lua_State luaState, int funcIndex, int n);

		[BizImport(cc)]
		public abstract int lua_iscfunction(lua_State luaState, int index);

		[BizImport(cc)]
		public abstract int lua_isinteger(lua_State luaState, int index);

		[BizImport(cc)]
		public abstract int lua_isnumber(lua_State luaState, int index);

		[BizImport(cc)]
		public abstract int lua_isstring(lua_State luaState, int index);

		[BizImport(cc)]
		public abstract int lua_isuserdata(lua_State luaState, int index);

		[BizImport(cc)]
		public abstract int lua_isyieldable(lua_State luaState);

		[BizImport(cc)]
		public abstract void lua_len(lua_State luaState, int index);

		[BizImport(cc)]
		public abstract int lua_load
			(lua_State luaState,
			lua_Reader reader,
			voidptr_t data,
			string chunkName,
			string mode);

		[BizImport(cc)]
		public abstract lua_State lua_newstate(lua_Alloc allocFunction, voidptr_t ud);

		[BizImport(cc)]
		public abstract lua_State lua_newthread(lua_State luaState);

		[BizImport(cc)]
		public abstract int lua_next(lua_State luaState, int index);

		[BizImport(cc)]
		public abstract int lua_pcallk
			(lua_State luaState,
			int nargs,
			int nresults,
			int errorfunc,
			lua_KContext ctx,
			lua_KFunction k);

		[BizImport(cc)]
		public abstract void lua_pushboolean(lua_State luaState, int value);

		[BizImport(cc)]
		public abstract void lua_pushcclosure(lua_State luaState, lua_CFunction f, int n);

		[BizImport(cc)]
		public abstract void lua_pushinteger(lua_State luaState, lua_Integer n);

		[BizImport(cc)]
		public abstract void lua_pushlightuserdata(lua_State luaState, voidptr_t udata);

		[BizImport(cc)]
		public abstract charptr_t lua_pushlstring(lua_State luaState, byte[] s, size_t len);

		[BizImport(cc)]
		public abstract void lua_pushnil(lua_State luaState);

		[BizImport(cc)]
		public abstract void lua_pushnumber(lua_State luaState, lua_Number number);

		[BizImport(cc)]
		public abstract int lua_pushthread(lua_State luaState);

		[BizImport(cc)]
		public abstract void lua_pushvalue(lua_State luaState, int index);

		[BizImport(cc)]
		public abstract int lua_rawequal(lua_State luaState, int index1, int index2);

		[BizImport(cc)]
		public abstract int lua_rawget(lua_State luaState, int index);

		[BizImport(cc)]
		public abstract int lua_rawgeti(lua_State luaState, int index, lua_Integer n);

		[BizImport(cc)]
		public abstract int lua_rawgetp(lua_State luaState, int index, voidptr_t p);

		[BizImport(cc)]
		public abstract size_t lua_rawlen(lua_State luaState, int index);

		[BizImport(cc)]
		public abstract void lua_rawset(lua_State luaState, int index);

		[BizImport(cc)]
		public abstract void lua_rawseti(lua_State luaState, int index, lua_Integer i);

		[BizImport(cc)]
		public abstract void lua_rawsetp(lua_State luaState, int index, voidptr_t p);

		[BizImport(cc)]
		public abstract void lua_rotate(lua_State luaState, int index, int n);

		[BizImport(cc)]
		public abstract void lua_setallocf(lua_State luaState, lua_Alloc f, voidptr_t ud);

		[BizImport(cc)]
		public abstract void lua_setfield(lua_State luaState, int index, string key);

		[BizImport(cc)]
		public abstract void lua_setglobal(lua_State luaState, string key);

		[BizImport(cc)]
		public abstract void lua_seti(lua_State luaState, int index, long n);

		[BizImport(cc)]
		public abstract void lua_setmetatable(lua_State luaState, int objIndex);

		[BizImport(cc)]
		public abstract void lua_settable(lua_State luaState, int index);

		[BizImport(cc)]
		public abstract void lua_settop(lua_State luaState, int newTop);

		[BizImport(cc)]
		public abstract charptr_t lua_setupvalue(lua_State luaState, int funcIndex, int n);

		[BizImport(cc)]
		public abstract int lua_status(lua_State luaState);

		[BizImport(cc)]
		public abstract size_t lua_stringtonumber(lua_State luaState, string s);

		[BizImport(cc)]
		public abstract int lua_toboolean(lua_State luaState, int index);

		[BizImport(cc)]
		public abstract lua_CFunction lua_tocfunction(lua_State luaState, int index);

		[BizImport(cc)]
		public abstract lua_Integer lua_tointegerx(lua_State luaState, int index, out int isNum);

		[BizImport(cc)]
		public abstract charptr_t lua_tolstring(lua_State luaState, int index, out size_t strLen);

		[BizImport(cc)]
		public abstract lua_Number lua_tonumberx(lua_State luaState, int index, out int isNum);

		[BizImport(cc)]
		public abstract voidptr_t lua_topointer(lua_State luaState, int index);

		[BizImport(cc)]
		public abstract lua_State lua_tothread(lua_State luaState, int index);

		[BizImport(cc)]
		public abstract voidptr_t lua_touserdata(lua_State luaState, int index);

		[BizImport(cc)]
		public abstract int lua_type(lua_State luaState, int index);

		[BizImport(cc)]
		public abstract charptr_t lua_typename(lua_State luaState, int type);

		[BizImport(cc)]
		public abstract voidptr_t lua_upvalueid(lua_State luaState, int funcIndex, int n);

		[BizImport(cc)]
		public abstract void lua_upvaluejoin(lua_State luaState, int funcIndex1, int n1, int funcIndex2, int n2);

		[BizImport(cc)]
		public abstract void lua_xmove(lua_State from, lua_State to, int n);

		[BizImport(cc)]
		public abstract int lua_yieldk(lua_State luaState,
			int nresults,
			lua_KContext ctx,
			lua_KFunction k);

		[BizImport(cc)]
		public abstract int luaL_argerror(lua_State luaState, int arg, string message);

		[BizImport(cc)]
		public abstract int luaL_callmeta(lua_State luaState, int obj, string e);

		[BizImport(cc)]
		public abstract void luaL_checkany(lua_State luaState, int arg);

		[BizImport(cc)]
		public abstract lua_Integer luaL_checkinteger(lua_State luaState, int arg);

		[BizImport(cc)]
		public abstract charptr_t luaL_checklstring(lua_State luaState, int arg, out size_t len);

		[BizImport(cc)]
		public abstract lua_Number luaL_checknumber(lua_State luaState, int arg);

		[BizImport(cc, Compatibility = true)]
		public abstract int luaL_checkoption(lua_State luaState, int arg, string def, string[] list);

		[BizImport(cc)]
		public abstract void luaL_checkstack(lua_State luaState, int sz, string message);

		[BizImport(cc)]
		public abstract void luaL_checktype(lua_State luaState, int arg, int type);

		[BizImport(cc)]
		public abstract voidptr_t luaL_checkudata(lua_State luaState, int arg, string tName);

		[BizImport(cc)]
		public abstract void luaL_checkversion_(lua_State luaState, lua_Number ver, size_t sz);

		[BizImport(cc)]
		public abstract int luaL_error(lua_State luaState, string message);

		[BizImport(cc)]
		public abstract int luaL_execresult(lua_State luaState, int stat);

		[BizImport(cc)]
		public abstract int luaL_fileresult(lua_State luaState, int stat, string fileName);

		[BizImport(cc)]
		public abstract int luaL_getmetafield(lua_State luaState, int obj, string e);

		[BizImport(cc)]
		public abstract int luaL_getsubtable(lua_State luaState, int index, string name);

		[BizImport(cc)]
		public abstract lua_Integer luaL_len(lua_State luaState, int index);

		[BizImport(cc)]
		public abstract int luaL_loadbufferx
			(lua_State luaState,
			byte[] buff,
			size_t sz,
			string name,
			string mode);

		[BizImport(cc)]
		public abstract int luaL_loadfilex(lua_State luaState, string name, string mode);

		[BizImport(cc)]
		public abstract int luaL_newmetatable(lua_State luaState, string name);

		[BizImport(cc)]
		public abstract lua_State luaL_newstate();

		[BizImport(cc)]
		public abstract void luaL_openlibs(lua_State luaState);

		[BizImport(cc)]
		public abstract lua_Integer luaL_optinteger(lua_State luaState, int arg, lua_Integer d);

		[BizImport(cc)]
		public abstract lua_Number luaL_optnumber(lua_State luaState, int arg, lua_Number d);

		[BizImport(cc)]
		public abstract int luaL_ref(lua_State luaState, int registryIndex);

		[BizImport(cc)]
		public abstract void luaL_requiref(lua_State luaState, string moduleName, lua_CFunction openFunction, int global);

		[BizImport(cc, Compatibility = true)]
		public abstract void luaL_setfuncs(lua_State luaState, [In] LuaRegister[] luaReg, int numUp);

		[BizImport(cc)]
		public abstract void luaL_setmetatable(lua_State luaState, string tName);

		[BizImport(cc)]
		public abstract voidptr_t luaL_testudata(lua_State luaState, int arg, string tName);

		[BizImport(cc)]
		public abstract charptr_t luaL_tolstring(lua_State luaState, int index, out size_t len);

		[BizImport(cc)]
		public abstract charptr_t luaL_traceback
			(lua_State luaState,
			lua_State luaState2,
			string message,
			int level);

		[BizImport(cc)]
		public abstract void luaL_unref(lua_State luaState, int registryIndex, int reference);

		[BizImport(cc)]
		public abstract void luaL_where(lua_State luaState, int level);
	}

	public abstract class Lua54NativeMethods : LuaNativeMethods
	{
		[BizImport(cc)]
		public abstract int lua_resume(lua_State luaState, lua_State from, int nargs, out int results);

		[BizImport(cc)]
		public abstract voidptr_t lua_newuserdatauv(lua_State luaState, size_t size, int nuvalue);

		[BizImport(cc)]
		public abstract void lua_setiuservalue(lua_State luaState, int index, int n);

		[BizImport(cc)]
		public abstract int lua_getiuservalue(lua_State luaState, int idx, int n);
	}

	public abstract class Lua53NativeMethods : LuaNativeMethods
	{
		[BizImport(cc)]
		public abstract int lua_resume(lua_State luaState, lua_State from, int nargs);

		[BizImport(cc)]
		public abstract voidptr_t lua_newuserdata(lua_State luaState, size_t size);

		[BizImport(cc)]
		public abstract void lua_setuservalue(lua_State luaState, int index);

		[BizImport(cc)]
		public abstract int lua_getuservalue(lua_State luaState, int idx);
	}
}
