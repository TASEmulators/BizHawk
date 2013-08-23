
using namespace System;
using namespace System::Runtime::InteropServices;
using namespace System::Reflection;
using namespace System::Collections;
using namespace System::Text;
using namespace System::Security;

// #include <vcclr.h>
// #define _WINNT_
// #include <WinDef.h>
#include <vcclr.h>
// #include <atlstr.h>
#include <stdio.h>
#using <mscorlib.dll>
#include <string.h>

#define LUA_BUILD_AS_DLL
#define LUA_LIB
#define LUA_CORE
#define lua_c
#define luac_c
#define loslib_c

extern "C"
{
#include "lua.h"
#include "lualib.h"
#include "lauxlib.h"
}

// Not sure of the purpose of this, but I'm keeping it -kevinh
static int tag = 0;

namespace Lua511 
{


#if 1
#undef LUA_TNONE
#undef LUA_TNIL
#undef LUA_TNUMBER
#undef LUA_TSTRING
#undef LUA_TBOOLEAN
#undef LUA_TTABLE
#undef LUA_TFUNCTION
#undef LUA_TUSERDATA
#undef LUA_TLIGHTUSERDATA

	/*
	 * Lua types for the API, returned by lua_type function
	 */
	public enum class LuaTypes 
	{
		LUA_TNONE=-1,
		LUA_TNIL=0,
		LUA_TNUMBER=3,
		LUA_TSTRING=4,
		LUA_TBOOLEAN=1,
		LUA_TTABLE=5,
		LUA_TFUNCTION=6,
		LUA_TUSERDATA=7,
		LUA_TLIGHTUSERDATA=2
	};

#endif

#if 1
#undef LUA_GCSTOP
#undef LUA_GCRESTART
#undef LUA_GCCOLLECT
#undef LUA_GCCOUNT
#undef LUA_GCCOUNTB
#undef LUA_GCSTEP
#undef LUA_GCSETPAUSE
#undef LUA_GCSETSTEPMUL

	/*
	 * Lua Garbage Collector options (param "what")
	 */
	public enum class LuaGCOptions
	{
		LUA_GCSTOP = 0,
		LUA_GCRESTART = 1,
		LUA_GCCOLLECT = 2,
		LUA_GCCOUNT = 3,
		LUA_GCCOUNTB = 4,
		LUA_GCSTEP = 5,
		LUA_GCSETPAUSE = 6,
		LUA_GCSETSTEPMUL = 7,
	};
#endif

#undef LUA_REGISTRYINDEX
#undef LUA_ENVIRONINDEX
#undef LUA_GLOBALSINDEX

	/*
	 * Special stack indexes
	 */
	public enum class LuaIndexes 
	{
		LUA_REGISTRYINDEX=-10000,
		LUA_ENVIRONINDEX=-10001,	
		LUA_GLOBALSINDEX=-10002	
	};


#if 0
	/*
	 * Structure used by the chunk reader
	 */
	// [ StructLayout( LayoutKind.Sequential )]
	public ref struct ReaderInfo
	{
		public String^ chunkData;
		public bool finished;
	};


	/*
	 * Delegate for chunk readers used with lua_load
	 */
	public delegate String^ LuaChunkReader(IntPtr luaState, ReaderInfo ^data, uint size);
#endif

	/*
	 * Delegate for functions passed to Lua as function pointers
	 */
	[System::Runtime::InteropServices::UnmanagedFunctionPointer(CallingConvention::Cdecl)]
	public delegate int LuaCSFunction(IntPtr luaState);

   // delegate for lua debug hook callback (by Reinhard Ostermeier)
	[System::Runtime::InteropServices::UnmanagedFunctionPointer(CallingConvention::Cdecl)]
   public delegate void LuaHookFunction(IntPtr luaState, IntPtr luaDebug);


	// To fix the strings:
	// http://support.microsoft.com/kb/311259

	public ref class LuaDLL 
	{
		// steffenj: BEGIN additional Lua API functions new in Lua 5.1
	public:

#define toState		((lua_State *) luaState.ToPointer())

		static int lua_gc(IntPtr luaState, LuaGCOptions what, int data)
		{
			return ::lua_gc(toState, (int) what, data);
		}

		static String^ lua_typename(IntPtr luaState, LuaTypes type)
		{
			return gcnew String(::lua_typename(toState, (int) type));
		}

#undef luaL_typename

		static String^ luaL_typename(IntPtr luaState, int stackPos)
		{
			return lua_typename(luaState, lua_type(luaState, stackPos));
		}

		static void luaL_error(IntPtr luaState, String^ message)
		{
			char *cs = (char *) Marshal::StringToHGlobalAnsi(message).ToPointer();

			::luaL_error(toState, cs);
			Marshal::FreeHGlobal(IntPtr(cs));
		}

        static void luaL_where(IntPtr luaState, int level)
		{
			::luaL_where(toState, level);
		}


		// Not yet wrapped
		// static String^ luaL_gsub(IntPtr luaState, String^ str, String^ pattern, String^ replacement);

#if 0
		// the functions below are still untested
		static void lua_getfenv(IntPtr luaState, int stackPos);
		static int lua_isfunction(IntPtr luaState, int stackPos);
		static int lua_islightuserdata(IntPtr luaState, int stackPos);
		static int lua_istable(IntPtr luaState, int stackPos);
		static int lua_isuserdata(IntPtr luaState, int stackPos);
		static int lua_lessthan(IntPtr luaState, int stackPos1, int stackPos2);
		static int lua_rawequal(IntPtr luaState, int stackPos1, int stackPos2);
		static int lua_setfenv(IntPtr luaState, int stackPos);
		static void lua_setfield(IntPtr luaState, int stackPos, String^ name);
		static int luaL_callmeta(IntPtr luaState, int stackPos, String^ name);
		// steffenj: END additional Lua API functions new in Lua 5.1
#endif

		// steffenj: BEGIN Lua 5.1.1 API change (lua_open replaced by luaL_newstate)
		static IntPtr luaL_newstate()
		{
			return IntPtr(::luaL_newstate());
		}

		static void lua_close(IntPtr luaState)
		{
			::lua_close(toState);
		}

		// steffenj: BEGIN Lua 5.1.1 API change (new function luaL_openlibs)
		static void luaL_openlibs(IntPtr luaState)
		{
			::luaL_openlibs(toState);
		}

		// Not yet wrapped
		// static int lua_objlen(IntPtr luaState, int stackPos);

		// steffenj: END Lua 5.1.1 API change (lua_strlen is now lua_objlen)
		// steffenj: BEGIN Lua 5.1.1 API change (lua_doString^ is now a macro luaL_dostring)
		static int luaL_loadstring(IntPtr luaState, String^ chunk)
		{
			char *cs = (char *) Marshal::StringToHGlobalAnsi(chunk).ToPointer();

			int result = ::luaL_loadstring(toState, cs);
			Marshal::FreeHGlobal(IntPtr(cs));

			return result;
		}

#undef luaL_dostring

		static int luaL_dostring(IntPtr luaState, String^ chunk)
		{
			int result = luaL_loadstring(luaState, chunk);
			if (result != 0)
				return result;

			return lua_pcall(luaState, 0, -1, 0);
		}

		/// <summary>DEPRECATED - use luaL_dostring(IntPtr luaState, string chunk) instead!</summary>
		static int lua_dostring(IntPtr luaState, String^ chunk)
		{
			return luaL_dostring(luaState, chunk);
		}

		// steffenj: END Lua 5.1.1 API change (lua_dostring is now a macro luaL_dostring)
		// steffenj: BEGIN Lua 5.1.1 API change (lua_newtable is gone, lua_createtable is new)
		static void lua_createtable(IntPtr luaState, int narr, int nrec)
		{
			::lua_createtable(toState, narr, nrec);
		}

#undef lua_newtable
		
		static void lua_newtable(IntPtr luaState)
		{
			lua_createtable(luaState, 0, 0);
		}

#undef luaL_dofile

		// steffenj: END Lua 5.1.1 API change (lua_newtable is gone, lua_createtable is new)
		// steffenj: BEGIN Lua 5.1.1 API change (lua_dofile now in LuaLib as luaL_dofile macro)
		static int luaL_dofile(IntPtr luaState, String^ fileName)
		{
			char *cs = (char *) Marshal::StringToHGlobalAnsi(fileName).ToPointer();

			int result = ::luaL_loadfile(toState, cs);
			
			//CP: Free filename string before return to ensure a file that isnt found still has the string freed (submitted by paul moore)
			//link: http://luaforge.net/forum/forum.php?thread_id=2825&forum_id=145
			Marshal::FreeHGlobal(IntPtr(cs));

			if (result != 0)
				return result;

			return ::lua_pcall(toState, 0, -1, 0);
		}

#undef lua_getglobal

		// steffenj: END Lua 5.1.1 API change (lua_dofile now in LuaLib as luaL_dofile)
		static void lua_getglobal(IntPtr luaState, String^ name) 
		{
			lua_pushstring(luaState, name);
			::lua_gettable(toState, (int) LuaIndexes::LUA_GLOBALSINDEX);
		}

#undef lua_setglobal

		static void lua_setglobal(IntPtr luaState, String^ name)
		{
			lua_pushstring(luaState,name);
			lua_insert(luaState,-2);
			lua_settable(luaState, (int) LuaIndexes::LUA_GLOBALSINDEX);
		}

		static void lua_settop(IntPtr luaState, int newTop)
		{
			::lua_settop(toState, newTop);
		}


#undef lua_pop

		static void lua_pop(IntPtr luaState, int amount)
		{
			lua_settop(luaState, -(amount) - 1);
		}

		static void lua_insert(IntPtr luaState, int newTop)
		{
			::lua_insert(toState, newTop);
		}

		static void lua_remove(IntPtr luaState, int index)
		{
			::lua_remove(toState, index);
		}

		static void lua_gettable(IntPtr luaState, int index)
		{
			::lua_gettable(toState, index);
		}


		static void lua_rawget(IntPtr luaState, int index)
		{
			::lua_rawget(toState, index);
		}


		static void lua_settable(IntPtr luaState, int index)
		{
			::lua_settable(toState, index);
		}


		static void lua_rawset(IntPtr luaState, int index)
		{
			::lua_rawset(toState, index);
		}

		static IntPtr lua_newthread(IntPtr luaState)
		{
			return IntPtr(::lua_newthread(toState));
		}

		static int lua_resume(IntPtr luaState, int narg)
		{
			return ::lua_resume(toState, narg);
		}

		static int lua_yield(IntPtr luaState, int nresults)
		{
			return ::lua_yield(toState, nresults);
		}


		static void lua_setmetatable(IntPtr luaState, int objIndex)
		{
			::lua_setmetatable(toState, objIndex);
		}


		static int lua_getmetatable(IntPtr luaState, int objIndex)
		{
			return ::lua_getmetatable(toState, objIndex);
		}


		static int lua_equal(IntPtr luaState, int index1, int index2)
		{
			return ::lua_equal(toState, index1, index2);
		}


		static void lua_pushvalue(IntPtr luaState, int index)
		{
			::lua_pushvalue(toState, index);
		}


		static void lua_replace(IntPtr luaState, int index)
		{
			::lua_replace(toState, index);
		}

		static int lua_gettop(IntPtr luaState)
		{
			return ::lua_gettop(toState);
		}


		static LuaTypes lua_type(IntPtr luaState, int index)
		{
			return (LuaTypes) ::lua_type(toState, index);
		}

#undef lua_isnil

		static bool lua_isnil(IntPtr luaState, int index)
		{
			return lua_type(luaState,index)==LuaTypes::LUA_TNIL;
		}

		static bool lua_isnumber(IntPtr luaState, int index)
		{
			return lua_type(luaState,index)==LuaTypes::LUA_TNUMBER;
		}

#undef lua_isboolean

		static bool lua_isboolean(IntPtr luaState, int index) 
		{
			return lua_type(luaState,index)==LuaTypes::LUA_TBOOLEAN;
		}

		static int luaL_ref(IntPtr luaState, int registryIndex)
		{
			return ::luaL_ref(toState, registryIndex);
		}

#undef lua_ref

		static int lua_ref(IntPtr luaState, int lockRef)
		{
			if(lockRef!=0) 
			{
				return luaL_ref(luaState, (int) LuaIndexes::LUA_REGISTRYINDEX);
			} 
			else return 0;
		}

		static void lua_rawgeti(IntPtr luaState, int tableIndex, int index)
		{
			::lua_rawgeti(toState, tableIndex, index);
		}

		static void lua_rawseti(IntPtr luaState, int tableIndex, int index)
		{
			::lua_rawseti(toState, tableIndex, index);
		}


		static IntPtr lua_newuserdata(IntPtr luaState, int size)
		{
			return IntPtr(::lua_newuserdata(toState, size));
		}


		static IntPtr lua_touserdata(IntPtr luaState, int index)
		{
			return IntPtr(::lua_touserdata(toState, index));
		}

#undef lua_getref

		static void lua_getref(IntPtr luaState, int reference)
		{
			lua_rawgeti(luaState, (int) LuaIndexes::LUA_REGISTRYINDEX,reference);
		}

		// Unwrapped
		// static void luaL_unref(IntPtr luaState, int registryIndex, int reference);

#undef lua_unref

		static void lua_unref(IntPtr luaState, int reference) 
		{
			::luaL_unref(toState, (int) LuaIndexes::LUA_REGISTRYINDEX,reference);
		}

		static bool lua_isstring(IntPtr luaState, int index)
		{
			return ::lua_isstring(toState, index) != 0;
		}


		static bool lua_iscfunction(IntPtr luaState, int index)
		{
			return ::lua_iscfunction(toState, index) != 0;
		}

		static void lua_pushnil(IntPtr luaState)
		{
			::lua_pushnil(toState);
		}



		static void lua_call(IntPtr luaState, int nArgs, int nResults)
		{
			::lua_call(toState, nArgs, nResults);
		}

		static int lua_pcall(IntPtr luaState, int nArgs, int nResults, int errfunc)
		{			
			return ::lua_pcall(toState, nArgs, nResults, errfunc);
		}

		// static int lua_rawcall(IntPtr luaState, int nArgs, int nResults)

		static IntPtr lua_tocfunction(IntPtr luaState, int index)
		{
			return IntPtr(::lua_tocfunction(toState, index));
		}

		static double lua_tonumber(IntPtr luaState, int index)
		{
			return ::lua_tonumber(toState, index);
		}


		static bool lua_toboolean(IntPtr luaState, int index)
		{
			return ::lua_toboolean(toState, index);
		}

		// unwrapped
		// was out strLen
		// static IntPtr lua_tolstring(IntPtr luaState, int index, [Out] int ^ strLen);

#undef lua_tostring

		static String^ lua_tostring(IntPtr luaState, int index)
		{
#if 1
			// FIXME use the same format string as lua i.e. LUA_NUMBER_FMT
			LuaTypes t = lua_type(luaState,index);
			
			if(t == LuaTypes::LUA_TNUMBER)
				return String::Format("{0}", lua_tonumber(luaState, index));
			else if(t == LuaTypes::LUA_TSTRING)
			{
				size_t strlen;

				const char *str = ::lua_tolstring(toState, index, &strlen);
				return Marshal::PtrToStringAnsi(IntPtr((char *) str), strlen);
			}
			else if(t == LuaTypes::LUA_TNIL)
				return nullptr;			// treat lua nulls to as C# nulls
			else
				return gcnew String("0");	// Because luaV_tostring does this
#else
			

			size_t strlen;

			// Note!  This method will _change_ the representation of the object on the stack to a string.
			// We do not want this behavior so we do the conversion ourselves
			const char *str = ::lua_tolstring(toState, index, &strlen);
            if (str)
				return Marshal::PtrToStringAnsi(IntPtr((char *) str), strlen);
            else
                return nullptr;            // treat lua nulls to as C# nulls
#endif
		}

        static void lua_atpanic(IntPtr luaState, LuaCSFunction^ panicf)
		{
			IntPtr p = Marshal::GetFunctionPointerForDelegate(panicf);
			::lua_atpanic(toState, (lua_CFunction) p.ToPointer());
		}

#if 0
		// no longer needed - all our functions are now stdcall calling convention
		static int stdcall_closure(lua_State *L) {
		  lua_CFunction fn = (lua_CFunction)lua_touserdata(L, lua_upvalueindex(1));
		  return fn(L);
		}
#endif
		
		static void lua_pushstdcallcfunction(IntPtr luaState, LuaCSFunction^ function)
		{
			IntPtr p = Marshal::GetFunctionPointerForDelegate(function);
			lua_pushcfunction(toState, (lua_CFunction) p.ToPointer());
		}


#if 0
		// not yet implemented
        static void lua_atlock(IntPtr luaState, LuaCSFunction^ lockf)
		{
			IntPtr p = Marshal::GetFunctionPointerForDelegate(lockf);
			::lua_atlock(toState, (lua_CFunction) p.ToPointer());
		}

        static void lua_atunlock(IntPtr luaState, LuaCSFunction^ unlockf);
#endif

		static void lua_pushnumber(IntPtr luaState, double number)
		{
			::lua_pushnumber(toState, number);
		}

		static void lua_pushboolean(IntPtr luaState, bool value)
		{
			::lua_pushboolean(toState, value);
		}

#if 0
		// Not yet wrapped
		static void lua_pushlstring(IntPtr luaState, String^ str, int size)
		{
			char *cs = (char *) Marshal::StringToHGlobalAnsi(str).ToPointer();

			//

			Marshal::FreeHGlobal(IntPtr(cs));
		}
#endif


		static void lua_pushstring(IntPtr luaState, String^ str)
		{
			char *cs = (char *) Marshal::StringToHGlobalAnsi(str).ToPointer();

			::lua_pushstring(toState, cs);

			Marshal::FreeHGlobal(IntPtr(cs));
		}


		static int luaL_newmetatable(IntPtr luaState, String^ meta)
		{
			char *cs = (char *) Marshal::StringToHGlobalAnsi(meta).ToPointer();

			int result = ::luaL_newmetatable(toState, cs);

			Marshal::FreeHGlobal(IntPtr(cs));

			return result;
		}


		// steffenj: BEGIN Lua 5.1.1 API change (luaL_getmetatable is now a macro using lua_getfield)
		static void lua_getfield(IntPtr luaState, int stackPos, String^ meta)
		{
			char *cs = (char *) Marshal::StringToHGlobalAnsi(meta).ToPointer();

			::lua_getfield(toState, stackPos, cs);

			Marshal::FreeHGlobal(IntPtr(cs));
		}

#undef luaL_getmetatable

		static void luaL_getmetatable(IntPtr luaState, String^ meta)
		{
			lua_getfield(luaState, (int) LuaIndexes::LUA_REGISTRYINDEX, meta);
		}

		static IntPtr luaL_checkudata(IntPtr luaState, int stackPos, String^ meta)
		{
			char *cs = (char *) Marshal::StringToHGlobalAnsi(meta).ToPointer();

			void *result = ::luaL_checkudata(toState, stackPos, cs);

			Marshal::FreeHGlobal(IntPtr(cs));

			return IntPtr(result);
		}

		static bool luaL_getmetafield(IntPtr luaState, int stackPos, String^ field)
		{
			char *cs = (char *) Marshal::StringToHGlobalAnsi(field).ToPointer();

			int result = ::luaL_getmetafield(toState, stackPos, cs);

			Marshal::FreeHGlobal(IntPtr(cs));

			return result != 0;
		}

		// wrapper not yet implemented
		// static int lua_load(IntPtr luaState, LuaChunkReader chunkReader, ref ReaderInfo data, String^ chunkName);

		static int luaL_loadbuffer(IntPtr luaState, String^ buff, String^ name)
		{
			char *cs1 = (char *) Marshal::StringToHGlobalAnsi(buff).ToPointer();
			char *cs2 = (char *) Marshal::StringToHGlobalAnsi(name).ToPointer();

			//CP: fix for MBCS, changed to use cs1's length (reported by qingrui.li)
			int result = ::luaL_loadbuffer(toState, cs1, strlen(cs1), cs2);

			Marshal::FreeHGlobal(IntPtr(cs1));
			Marshal::FreeHGlobal(IntPtr(cs2));

			return result;
		}

		static int luaL_loadfile(IntPtr luaState, String^ filename)
		{
			char *cs = (char *) Marshal::StringToHGlobalAnsi(filename).ToPointer();
			int result = ::luaL_loadfile(toState, cs);

			Marshal::FreeHGlobal(IntPtr(cs));

			return result;
		}

		static void lua_error(IntPtr luaState)
		{
			::lua_error(toState);
		}


		static bool lua_checkstack(IntPtr luaState,int extra)
		{
			return ::lua_checkstack(toState, extra) != 0;
		}


		static int lua_next(IntPtr luaState,int index)
		{
			return ::lua_next(toState, index);
		}




		static void lua_pushlightuserdata(IntPtr luaState, IntPtr udata)
		{
			::lua_pushlightuserdata(toState, udata.ToPointer());
		}

		static int luanet_rawnetobj(IntPtr luaState,int obj)
		{
			int *udata= (int *) lua_touserdata(luaState, obj).ToPointer();
			if(udata!=NULL) return *udata;
			return -1;
		}


      // lua debug hook functions added by Reinhard Ostermeier

      static int lua_sethook(IntPtr luaState, LuaHookFunction^ func, int mask, int count)
      {
         IntPtr p;
         if (func == nullptr)
         {
            p = IntPtr::Zero;
         }
         else
         {
            p = Marshal::GetFunctionPointerForDelegate(func);
         }
         return ::lua_sethook(toState, (lua_Hook)p.ToPointer(), mask, count);
      }

      static int lua_gethookmask(IntPtr luaState)
      {
         return ::lua_gethookmask(toState);
      }

      static int lua_gethookcount(IntPtr luaState)
      {
         return ::lua_gethookcount(toState);
      }

      static int lua_getstack(IntPtr luaState, int level, IntPtr luaDebug)
      {
         return ::lua_getstack(toState, level, (lua_Debug*)luaDebug.ToPointer());
      }

      static int lua_getinfo(IntPtr luaState, String^ what, IntPtr luaDebug)
      {
         char *cs = (char *) Marshal::StringToHGlobalAnsi(what).ToPointer();
         int ret = ::lua_getinfo(toState, cs, (lua_Debug*)luaDebug.ToPointer());
			Marshal::FreeHGlobal(IntPtr(cs));
         return ret;
      }

      static String^ lua_getlocal(IntPtr luaState, IntPtr luaDebug, int n)
      {
         const char* str = ::lua_getlocal(toState, (lua_Debug*)luaDebug.ToPointer(), n);
         if (str == NULL)
         {
            return nullptr;
         }
         else
         {
            return gcnew String(str);
         }
      }

      static String^ lua_setlocal(IntPtr luaState, IntPtr luaDebug, int n)
      {
         const char* str = ::lua_setlocal(toState, (lua_Debug*)luaDebug.ToPointer(), n);
         if(str == NULL)
         {
            return nullptr;
         }
         else
         {
            return gcnew String(str);
         }
      }

      static String^ lua_getupvalue(IntPtr luaState, int funcindex, int n)
      {
         const char* str = ::lua_getupvalue(toState, funcindex, n);
         if(str == NULL)
         {
            return nullptr;
         }
         else
         {
            return gcnew String(str);
         }
      }

      static String^ lua_setupvalue(IntPtr luaState, int funcindex, int n)
      {
         const char* str = ::lua_setupvalue(toState, funcindex, n);
         if(str == NULL)
         {
            return nullptr;
         }
         else
         {
            return gcnew String(str);
         }
      }

      // end of lua debug hook functions

private:

		// Starting with 5.1 the auxlib version of checkudata throws an exception if the type isn't right
		// Instead, we want to run our own version that checks the type and just returns null for failure
		static void *checkudata_raw(lua_State *L, int ud, const char *tname)
		{
			void *p = ::lua_touserdata(L, ud);

			  if (p != NULL) 
			  {  /* value is a userdata? */
				  if (::lua_getmetatable(L, ud)) 
				  { 
					int isEqual;

					/* does it have a metatable? */
					::lua_getfield(L, (int) LuaIndexes::LUA_REGISTRYINDEX, tname);  /* get correct metatable */

					isEqual = lua_rawequal(L, -1, -2);

					// NASTY - we need our own version of the lua_pop macro
					// lua_pop(L, 2);  /* remove both metatables */
					::lua_settop(L, -(2) - 1);

					if (isEqual)   /* does it have the correct mt? */
						return p;
				  }
			  }
		  
		  return NULL;
		}


public:

		static int luanet_checkudata(IntPtr luaState, int ud, String ^tname)
		{
			char *cs = (char *) Marshal::StringToHGlobalAnsi(tname).ToPointer();

		    int *udata=(int*) checkudata_raw(toState, ud, cs);

			Marshal::FreeHGlobal(IntPtr(cs));

		    if(udata!=NULL) return *udata;
		    return -1;
		}


		static bool luaL_checkmetatable(IntPtr luaState,int index)
		{
			int retVal=0;

			if(lua_getmetatable(luaState,index)!=0) 
			{
				lua_pushlightuserdata(luaState, IntPtr(&tag));
				lua_rawget(luaState, -2);
				retVal = !lua_isnil(luaState, -1);
				lua_settop(luaState, -3);
			}
			return retVal;
		}

		static IntPtr luanet_gettag() 
		{
			return IntPtr(&tag);
		}

		static void luanet_newudata(IntPtr luaState,int val)
		{
			int* pointer=(int*) lua_newuserdata(luaState, sizeof(int)).ToPointer();
			*pointer=val;
		}

		static int luanet_tonetobject(IntPtr luaState,int index)
		{
			int *udata;

			if(lua_type(luaState,index)==LuaTypes::LUA_TUSERDATA) 
			{
				if(luaL_checkmetatable(luaState, index)) 
				{
				udata=(int*) lua_touserdata(luaState,index).ToPointer();
				if(udata!=NULL) 
					return *udata; 
				}

			udata=(int*)checkudata_raw(toState,index, "luaNet_class");
			if(udata!=NULL) return *udata;
			udata=(int*)checkudata_raw(toState,index, "luaNet_searchbase");
			if(udata!=NULL) return *udata;
			udata=(int*)checkudata_raw(toState,index, "luaNet_function");
			if(udata!=NULL) return *udata;
			}
			return -1;
		}


#if 0


		[DllImport(STUBDLL,CallingConvention=CallingConvention.Cdecl)]
		
#endif
	};
}
