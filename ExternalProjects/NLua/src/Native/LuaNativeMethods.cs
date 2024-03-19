using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

using charptr_t = System.IntPtr;
using lua_CFunction = System.IntPtr;
using lua_Integer = System.Int64;
using lua_KContext = System.IntPtr;
using lua_KFunction = System.IntPtr;
using lua_Number = System.Double;
using lua_State = System.IntPtr;
using size_t = System.UIntPtr;
using voidptr_t = System.IntPtr;

#pragma warning disable SA1121 // Use built-in type alias

namespace NLua.Native
{
	[SuppressUnmanagedCodeSecurity]
	internal unsafe class LuaNativeMethods
	{
		// TODO: Get rid of this once we're .NET Core/.NET
		private static class Mershul
		{
			public static charptr_t StringToCoTaskMemUTF8(string s)
			{
				if (s == null)
				{
					return charptr_t.Zero;
				}

				var nb = Encoding.UTF8.GetMaxByteCount(s.Length);
				var ptr = Marshal.AllocCoTaskMem(checked(nb + 1));

				fixed (char* c = s)
				{
					var pbMem = (byte*)ptr;
					var nbWritten = Encoding.UTF8.GetBytes(c, s.Length, pbMem!, nb);
					pbMem[nbWritten] = 0;
				}

				return ptr;
			}
		}

		internal bool IsLua53;

		internal delegate* unmanaged[Cdecl]<lua_State, lua_CFunction, lua_CFunction> lua_atpanic;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public lua_CFunction AtPanic(lua_State luaState, lua_CFunction panicf) => lua_atpanic(luaState, panicf);

		internal delegate* unmanaged[Cdecl]<lua_State, int, int> lua_checkstack;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int CheckStack(lua_State luaState, int extra) => lua_checkstack(luaState, extra);

		internal delegate* unmanaged[Cdecl]<lua_State, void> lua_close;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Close(lua_State luaState) => lua_close(luaState);

		internal delegate* unmanaged[Cdecl]<lua_State, int, int, int, int> lua_compare;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int Compare(lua_State luaState, int index1, int index2, int op) => lua_compare(luaState, index1, index2, op);

		internal delegate* unmanaged[Cdecl]<lua_State, int, int, void> lua_createtable;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void CreateTable(lua_State luaState, int elements, int records) => lua_createtable(luaState, elements, records);

		internal delegate* unmanaged[Cdecl]<lua_State, int> lua_error;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int Error(lua_State luaState) => lua_error(luaState);

		internal delegate* unmanaged[Cdecl]<lua_State, int, charptr_t, int> lua_getfield;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int GetField(lua_State luaState, int index, string k)
		{
			var _k = Mershul.StringToCoTaskMemUTF8(k);
			try
			{
				return lua_getfield(luaState, index, _k);
			}
			finally
			{
				Marshal.FreeCoTaskMem(_k);
			}
		}

		internal delegate* unmanaged[Cdecl]<lua_State, charptr_t, int> lua_getglobal;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int GetGlobal(lua_State luaState, string name)
		{
			var _name = Mershul.StringToCoTaskMemUTF8(name);
			try
			{
				return lua_getglobal(luaState, _name);
			}
			finally
			{
				Marshal.FreeCoTaskMem(_name);
			}
		}

		internal delegate* unmanaged[Cdecl]<lua_State, int, int> lua_getmetatable;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int GetMetaTable(lua_State luaState, int index) => lua_getmetatable(luaState, index);

		internal delegate* unmanaged[Cdecl]<lua_State, int, int> lua_gettable;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int GetTable(lua_State luaState, int index) => lua_gettable(luaState, index);

		internal delegate* unmanaged[Cdecl]<lua_State, int> lua_gettop;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int GetTop(lua_State luaState) => lua_gettop(luaState);

		internal delegate* unmanaged[Cdecl]<lua_State, int, int> lua_isinteger;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int IsInteger(lua_State luaState, int index) => lua_isinteger(luaState, index);

		internal delegate* unmanaged[Cdecl]<lua_State, int, int> lua_isnumber;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int IsNumber(lua_State luaState, int index) => lua_isnumber(luaState, index);

		internal delegate* unmanaged[Cdecl]<lua_State, int, int> lua_isstring;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int IsString(lua_State luaState, int index) => lua_isstring(luaState, index);

		internal delegate* unmanaged[Cdecl]<lua_State, lua_State> lua_newthread;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public lua_State NewThread(lua_State luaState) => lua_newthread(luaState);

		internal delegate* unmanaged[Cdecl]<lua_State, size_t, int, voidptr_t> lua_newuserdatauv;
		internal delegate* unmanaged[Cdecl]<lua_State, size_t, voidptr_t> lua_newuserdata;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public voidptr_t NewUserData(lua_State luaState, size_t size) => lua_newuserdatauv != null
			? lua_newuserdatauv(luaState, size, 1)
			: lua_newuserdata(luaState, size);

		internal delegate* unmanaged[Cdecl]<lua_State, int, int> lua_next;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int Next(lua_State luaState, int index) => lua_next(luaState, index);

		internal delegate* unmanaged[Cdecl]<lua_State, int, int, int, lua_KContext, lua_KFunction, int> lua_pcallk;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int PCallK(lua_State luaState, int nargs, int nresults, int errorfunc, lua_KContext ctx, lua_KFunction k) => lua_pcallk(luaState, nargs, nresults, errorfunc, ctx, k);

		internal delegate* unmanaged[Cdecl]<lua_State, int, void> lua_pushboolean;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void PushBoolean(lua_State luaState, int value) => lua_pushboolean(luaState, value);

		internal delegate* unmanaged[Cdecl]<lua_State, lua_CFunction, int, void> lua_pushcclosure;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void PushCClosure(lua_State luaState, lua_CFunction f, int n) => lua_pushcclosure(luaState, f, n);

		internal delegate* unmanaged[Cdecl]<lua_State, lua_Integer, void> lua_pushinteger;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void PushInteger(lua_State luaState, lua_Integer n) => lua_pushinteger(luaState, n);

		internal delegate* unmanaged[Cdecl]<lua_State, voidptr_t, void> lua_pushlightuserdata;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void PushLightUserData(lua_State luaState, voidptr_t udata) => lua_pushlightuserdata(luaState, udata);

		internal delegate* unmanaged[Cdecl]<lua_State, charptr_t, size_t, charptr_t> lua_pushlstring;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public charptr_t PushLString(lua_State luaState, ReadOnlySpan<byte> s, size_t len)
		{
			fixed (byte* _s = s)
			{
				return lua_pushlstring(luaState, (charptr_t)_s, len);
			}
		}

		internal delegate* unmanaged[Cdecl]<lua_State, void> lua_pushnil;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void PushNil(lua_State luaState) => lua_pushnil(luaState);

		internal delegate* unmanaged[Cdecl]<lua_State, lua_Number, void> lua_pushnumber;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void PushNumber(lua_State luaState, lua_Number number) => lua_pushnumber(luaState, number);

		internal delegate* unmanaged[Cdecl]<lua_State, int> lua_pushthread;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int PushThread(lua_State luaState) => lua_pushthread(luaState);

		internal delegate* unmanaged[Cdecl]<lua_State, int, void> lua_pushvalue;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void PushValue(lua_State luaState, int index) => lua_pushvalue(luaState, index);

		internal delegate* unmanaged[Cdecl]<lua_State, int, int, int> lua_rawequal;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int RawEqual(lua_State luaState, int index1, int index2) => lua_rawequal(luaState, index1, index2);

		internal delegate* unmanaged[Cdecl]<lua_State, int, int> lua_rawget;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int RawGet(lua_State luaState, int index) => lua_rawget(luaState, index);

		internal delegate* unmanaged[Cdecl]<lua_State, int, lua_Integer, int> lua_rawgeti;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int RawGetI(lua_State luaState, int index, lua_Integer n) => lua_rawgeti(luaState, index, n);

		internal delegate* unmanaged[Cdecl]<lua_State, int, void> lua_rawset;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void RawSet(lua_State luaState, int index) => lua_rawset(luaState, index);

		internal delegate* unmanaged[Cdecl]<lua_State, int, lua_Integer, void> lua_rawseti;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void RawSetI(lua_State luaState, int index, lua_Integer i) => lua_rawseti(luaState, index, i);

		internal delegate* unmanaged[Cdecl]<lua_State, lua_State, int, out int, int> lua_resume_54;
		internal delegate* unmanaged[Cdecl]<lua_State, lua_State, int, int> lua_resume_53;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int Resume(lua_State luaState, lua_State from, int nargs) => lua_resume_54 != null
			? lua_resume_54(luaState, from, nargs, out _)
			: lua_resume_53(luaState, from, nargs);

		internal delegate* unmanaged[Cdecl]<lua_State, int, int, void> lua_rotate;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Rotate(lua_State luaState, int index, int n) => lua_rotate(luaState, index, n);

		internal delegate* unmanaged[Cdecl]<lua_State, charptr_t, void> lua_setglobal;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetGlobal(lua_State luaState, string key)
		{
			var _key = Mershul.StringToCoTaskMemUTF8(key);
			try
			{
				lua_setglobal(luaState, _key);
			}
			finally
			{
				Marshal.FreeCoTaskMem(_key);
			}
		}

		internal delegate* unmanaged[Cdecl]<lua_State, int, void> lua_setmetatable;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetMetaTable(lua_State luaState, int objIndex) => lua_setmetatable(luaState, objIndex);

		internal delegate* unmanaged[Cdecl]<lua_State, int, void> lua_settable;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetTable(lua_State luaState, int index) => lua_settable(luaState, index);

		internal delegate* unmanaged[Cdecl]<lua_State, int, void> lua_settop;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetTop(lua_State luaState, int newTop) => lua_settop(luaState, newTop);

		internal delegate* unmanaged[Cdecl]<lua_State, int, int> lua_toboolean;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int ToBoolean(lua_State luaState, int index) => lua_toboolean(luaState, index);

		internal delegate* unmanaged[Cdecl]<lua_State, int, out int, lua_Integer> lua_tointegerx;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public lua_Integer ToIntegerX(lua_State luaState, int index, out int isNum) => lua_tointegerx(luaState, index, out isNum);

		internal delegate* unmanaged[Cdecl]<lua_State, int, out size_t, charptr_t> lua_tolstring;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public charptr_t ToLString(lua_State luaState, int index, out size_t strLen) => lua_tolstring(luaState, index, out strLen);

		internal delegate* unmanaged[Cdecl]<lua_State, int, out int, lua_Number> lua_tonumberx;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public lua_Number ToNumberX(lua_State luaState, int index, out int isNum) => lua_tonumberx(luaState, index, out isNum);

		internal delegate* unmanaged[Cdecl]<lua_State, int, lua_State> lua_tothread;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public lua_State ToThread(lua_State luaState, int index) => lua_tothread(luaState, index);

		internal delegate* unmanaged[Cdecl]<lua_State, int, voidptr_t> lua_touserdata;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public voidptr_t ToUserData(lua_State luaState, int index) => lua_touserdata(luaState, index);

		internal delegate* unmanaged[Cdecl]<lua_State, int, int> lua_type;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int Type(lua_State luaState, int index) => lua_type(luaState, index);

		internal delegate* unmanaged[Cdecl]<lua_State, lua_State, int, void> lua_xmove;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void XMove(lua_State from, lua_State to, int n) => lua_xmove(from, to, n);

		internal delegate* unmanaged[Cdecl]<lua_State, int, lua_KContext, lua_KFunction, int> lua_yieldk;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int YieldK(lua_State luaState, int nresults, lua_KContext ctx, lua_KFunction k) => lua_yieldk(luaState, nresults, ctx, k);

		internal delegate* unmanaged[Cdecl]<lua_State, int, charptr_t, int> luaL_getmetafield;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int LGetMetaField(lua_State luaState, int obj, string e)
		{
			var _e = Mershul.StringToCoTaskMemUTF8(e);
			try
			{
				return luaL_getmetafield(luaState, obj, _e);
			}
			finally
			{
				Marshal.FreeCoTaskMem(_e);
			}
		}

		internal delegate* unmanaged[Cdecl]<lua_State, charptr_t, size_t, charptr_t, charptr_t, int> luaL_loadbufferx;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int LLoadBufferX(lua_State luaState, byte[] buff, size_t sz, string name, string mode)
		{
			fixed (byte* _buff = buff)
			{
				var _name = charptr_t.Zero;
				var _mode = charptr_t.Zero;
				try
				{
					_name = Mershul.StringToCoTaskMemUTF8(name);
					_mode = Mershul.StringToCoTaskMemUTF8(mode);
					return luaL_loadbufferx(luaState, (charptr_t)_buff, sz, _name, _mode);
				}
				finally
				{
					Marshal.FreeCoTaskMem(_name);
					Marshal.FreeCoTaskMem(_mode);
				}
			}
		}

		internal delegate* unmanaged[Cdecl]<lua_State, charptr_t, charptr_t, int> luaL_loadfilex;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int LLoadFileX(lua_State luaState, string name, string mode)
		{
			var _name = charptr_t.Zero;
			var _mode = charptr_t.Zero;
			try
			{
				_name = Mershul.StringToCoTaskMemUTF8(name);
				_mode = Mershul.StringToCoTaskMemUTF8(mode);
				return luaL_loadfilex(luaState, _name, _mode);
			}
			finally
			{
				Marshal.FreeCoTaskMem(_name);
				Marshal.FreeCoTaskMem(_mode);
			}
		}

		internal delegate* unmanaged[Cdecl]<lua_State, charptr_t, int> luaL_newmetatable;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int LNewMetaTable(lua_State luaState, string name)
		{
			var _name = Mershul.StringToCoTaskMemUTF8(name);
			try
			{
				return luaL_newmetatable(luaState, _name);
			}
			finally
			{
				Marshal.FreeCoTaskMem(_name);
			}
		}

		internal delegate* unmanaged[Cdecl]<lua_State> luaL_newstate;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public lua_State LNewState() => luaL_newstate();

		internal delegate* unmanaged[Cdecl]<lua_State, void> luaL_openlibs;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void LOpenLibs(lua_State luaState) => luaL_openlibs(luaState);

		internal delegate* unmanaged[Cdecl]<lua_State, int, int> luaL_ref;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int LRef(lua_State luaState, int registryIndex) => luaL_ref(luaState, registryIndex);

		internal delegate* unmanaged[Cdecl]<lua_State, int, out size_t, charptr_t> luaL_tolstring;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public charptr_t LToLString(lua_State luaState, int index, out size_t len) => luaL_tolstring(luaState, index, out len);

		internal delegate* unmanaged[Cdecl]<lua_State, int, int, void> luaL_unref;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void LUnref(lua_State luaState, int registryIndex, int reference) => luaL_unref(luaState, registryIndex, reference);

		internal delegate* unmanaged[Cdecl]<lua_State, int, void> luaL_where;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void LWhere(lua_State luaState, int level) => luaL_where(luaState, level);
	}
}
