// #include <vcclr.h>
// #define _WINNT_
// #include <WinDef.h>
// #include <vcclr.h>
// #include <atlstr.h>

/**
 * lsthiros 1/15/2018 - Large rewrite for mono portability:
 * Rewrote most of this file to get rid of all traces of Managed C++
 * and all of its associated anti-patterns. Rewrote functions such
 * that they could be easily extracted with tool like SWIG to generate
 * robust bindings for Mono/.net. Callbacks from lua to C# were tricky
 * but were handled via lua's upvalue system that allows closure
 * behaviour in C/C++.
 */

#include "LuaDLL.hpp"

#include <stdio.h>
#include <string>

// #define LUA_BUILD_AS_DLL
#define LUA_LIB
#define LUA_CORE
#define lua_c
#define luac_c
#define loslib_c

extern "C"
{
#include "lualib.h"
#include "lauxlib.h"

	
/**
 * Make these available in an interface
 */
int iuplua_open(lua_State * L);
int iupcontrolslua_open(lua_State * L);
int luaopen_winapi(lua_State * L);

//luasocket
int luaopen_socket_core(lua_State *L);
int luaopen_mime_core(lua_State *L);

static void luaperks(lua_State *L)
{
	#ifdef LUAPERKS
		iuplua_open(L);
		iupcontrolslua_open(L);
		luaopen_winapi(L);

		//luasocket - yeah, have to open this in a weird way
		lua_pushcfunction(L,luaopen_socket_core);
		lua_setglobal(L,"tmp");
		luaL_dostring(L, "package.preload[\"socket.core\"] = _G.tmp");
		lua_pushcfunction(L,luaopen_mime_core);
		lua_setglobal(L,"tmp");
		luaL_dostring(L, "package.preload[\"mime.core\"] = _G.tmp");
	#endif
}

}

// Not sure of the purpose of this, but I'm keeping it -kevinh
static int tag = 0;

namespace Lua511
{
#if 1

#endif

    
    // lsthiros 1/15/2018 - Removed "if 0"'d code. Didn't look like it would ever be useful.
    
    // lsthiros 1/15/2018 - Removed old Managed C++ delegates. They aren't portable at all, and
    // Managed C++ is all but deprecated at this point.
    static LuaHook *hookBack = NULL;
    static LuaCallback *panic_cb = NULL;

    // lsthiros 1/15/2018 - Class definition moved to header file. Managed C++ antipatterns were
    // forcing it to stay here earlier.
		// steffenj: BEGIN additional Lua API functions new in Lua 5.1
        int LuaDLL::lua_gc(lua_State *luaState, LuaGCOptions what, int data)
		{
			return ::lua_gc(luaState, (int) what, data);
		}

        std::string LuaDLL::lua_typename(lua_State *luaState, LuaTypes type)
		{
            return std::string(::lua_typename(luaState, (int) type));
		}

#undef luaL_typename

         std::string LuaDLL::luaL_typename(lua_State *luaState, int stackPos)
		{
			return lua_typename(luaState, lua_type(luaState, stackPos));
		}

        void LuaDLL::luaL_error(lua_State *luaState, const std::string &message)
		{
			const char *cs = message.c_str();
            ::luaL_error(luaState, cs);
		}

        void LuaDLL::luaL_where(lua_State *luaState, int level)
		{
            ::luaL_where(luaState, level);
		}


		// Not yet wrapped
		// static std::string luaL_gsub(lua_State *luaState, std::string str, std::string pattern, std::string replacement);

#if 0
		// the functions below are still untested
		static void lua_getfenv(lua_State *luaState, int stackPos);
		static int lua_isfunction(lua_State *luaState, int stackPos);
		static int lua_islightuserdata(lua_State *luaState, int stackPos);
		static int lua_istable(lua_State *luaState, int stackPos);
		static int lua_isuserdata(lua_State *luaState, int stackPos);
		static int lua_lessthan(lua_State *luaState, int stackPos1, int stackPos2);
		static int lua_rawequal(lua_State *luaState, int stackPos1, int stackPos2);
		static int lua_setfenv(lua_State *luaState, int stackPos);
		static void lua_setfield(lua_State *luaState, int stackPos, std::string name);
		static int luaL_callmeta(lua_State *luaState, int stackPos, std::string name);
		// steffenj: END additional Lua API functions new in Lua 5.1
#endif

		// steffenj: BEGIN Lua 5.1.1 API change (lua_open replaced by luaL_newstate)
		lua_State *LuaDLL::luaL_newstate()
		{
			return ::luaL_newstate();
		}

		void LuaDLL::lua_close(lua_State *luaState)
		{
			::lua_close(luaState);
		}

		// steffenj: BEGIN Lua 5.1.1 API change (new function luaL_openlibs)
		void LuaDLL::luaL_openlibs(lua_State *luaState)
		{
            ::luaL_openlibs(luaState);
            ::luaperks(luaState);
		}

		// Not yet wrapped
		// static int lua_objlen(lua_State *luaState, int stackPos);

		// steffenj: END Lua 5.1.1 API change (lua_strlen is now lua_objlen)
		// steffenj: BEGIN Lua 5.1.1 API change (lua_dostd::string is now a macro luaL_dostring)
        int LuaDLL::luaL_loadstring(lua_State *luaState, const std::string &chunk)
		{
			const char *cs = chunk.c_str();
            int result = ::luaL_loadstring(luaState, cs);
			return result;
		}

#undef luaL_dostring

        int LuaDLL::luaL_dostring(lua_State *luaState, const std::string &chunk)
		{
			int result = luaL_loadstring(luaState, chunk);
			if (result != 0)
				return result;

			return lua_pcall(luaState, 0, -1, 0);
		}

		/// <summary>DEPRECATED - use luaL_dostring(lua_State *luaState, string chunk) instead!</summary>
        int LuaDLL::lua_dostring(lua_State *luaState, const std::string &chunk)
		{
			return luaL_dostring(luaState, chunk);
		}

		// steffenj: END Lua 5.1.1 API change (lua_dostring is now a macro luaL_dostring)
		// steffenj: BEGIN Lua 5.1.1 API change (lua_newtable is gone, lua_createtable is new)
		void LuaDLL::lua_createtable(lua_State *luaState, int narr, int nrec)
		{
			::lua_createtable(luaState, narr, nrec);
		}

#undef lua_newtable
		
		void LuaDLL::lua_newtable(lua_State *luaState)
		{
			lua_createtable(luaState, 0, 0);
		}

#undef luaL_dofile

		// steffenj: END Lua 5.1.1 API change (lua_newtable is gone, lua_createtable is new)
		// steffenj: BEGIN Lua 5.1.1 API change (lua_dofile now in LuaLib as luaL_dofile macro)
        int LuaDLL::luaL_dofile(lua_State *luaState, const std::string &fileName)
		{
			const char *cs = fileName.c_str();

			int result = ::luaL_loadfile(luaState, cs);

			if (result != 0)
				return result;

			return ::lua_pcall(luaState, 0, -1, 0);
		}

#undef lua_getglobal

		// steffenj: END Lua 5.1.1 API change (lua_dofile now in LuaLib as luaL_dofile)
        void LuaDLL::lua_getglobal(lua_State *luaState, const std::string &name)
		{
			lua_pushstring(luaState, name);
			::lua_gettable(luaState, (int) LuaIndexes::LUA_GLOBALSINDEX);
		}

#undef lua_setglobal

        void LuaDLL::lua_setglobal(lua_State *luaState, const std::string &name)
		{
			lua_pushstring(luaState,name);
			lua_insert(luaState,-2);
			lua_settable(luaState, (int) LuaIndexes::LUA_GLOBALSINDEX);
		}

		void LuaDLL::lua_settop(lua_State *luaState, int newTop)
		{
			::lua_settop(luaState, newTop);
		}


#undef lua_pop

		void LuaDLL::lua_pop(lua_State *luaState, int amount)
		{
            LuaDLL::lua_settop(luaState, -(amount) - 1);
		}

		void LuaDLL::lua_insert(lua_State *luaState, int newTop)
		{
			::lua_insert(luaState, newTop);
		}

		void LuaDLL::lua_remove(lua_State *luaState, int index)
		{
			::lua_remove(luaState, index);
		}

		void LuaDLL::lua_gettable(lua_State *luaState, int index)
		{
			::lua_gettable(luaState, index);
		}


		void LuaDLL::lua_rawget(lua_State *luaState, int index)
		{
			::lua_rawget(luaState, index);
		}


		void LuaDLL::lua_settable(lua_State *luaState, int index)
		{
			::lua_settable(luaState, index);
		}


		void LuaDLL::lua_rawset(lua_State *luaState, int index)
		{
			::lua_rawset(luaState, index);
		}

		lua_State *LuaDLL::lua_newthread(lua_State *luaState)
		{
			return ::lua_newthread(luaState);
		}

		int LuaDLL::lua_resume(lua_State *luaState, int narg)
		{
			return ::lua_resume(luaState, narg);
		}

		int LuaDLL::lua_yield(lua_State *luaState, int nresults)
		{
			return ::lua_yield(luaState, nresults);
		}


        void LuaDLL::lua_setmetatable(lua_State *luaState, int objIndex)
		{
			::lua_setmetatable(luaState, objIndex);
		}


		int LuaDLL::lua_getmetatable(lua_State *luaState, int objIndex)
		{
			return ::lua_getmetatable(luaState, objIndex);
		}


		int LuaDLL::lua_equal(lua_State *luaState, int index1, int index2)
		{
			return ::lua_equal(luaState, index1, index2);
		}


		void LuaDLL::lua_pushvalue(lua_State *luaState, int index)
		{
			::lua_pushvalue(luaState, index);
		}


		void LuaDLL::lua_replace(lua_State *luaState, int index)
		{
			::lua_replace(luaState, index);
		}

		int LuaDLL::lua_gettop(lua_State *luaState)
		{
			return ::lua_gettop(luaState);
		}


		LuaTypes LuaDLL::lua_type(lua_State *luaState, int index)
		{
			return (LuaTypes) ::lua_type(luaState, index);
		}

#undef lua_isnil

		bool LuaDLL::lua_isnil(lua_State *luaState, int index)
		{
			return lua_type(luaState,index)==LuaTypes::LUA_TNIL;
		}

		bool LuaDLL::lua_isnumber(lua_State *luaState, int index)
		{
			return lua_type(luaState,index)==LuaTypes::LUA_TNUMBER;
		}

#undef lua_isboolean

		bool LuaDLL::lua_isboolean(lua_State *luaState, int index)
		{
            return LuaDLL::lua_type(luaState,index)==LuaTypes::LUA_TBOOLEAN;
		}

    int LuaDLL::luaL_ref(lua_State *luaState, int registryIndex)
		{
			return ::luaL_ref(luaState, registryIndex);
		}

#undef lua_ref

		int LuaDLL::lua_ref(lua_State *luaState, int lockRef)
		{
			if(lockRef!=0) 
			{
                return LuaDLL::luaL_ref(luaState, (int) LuaIndexes::LUA_REGISTRYINDEX);
			} 
			else return 0;
		}

		void LuaDLL::lua_rawgeti(lua_State *luaState, int tableIndex, int index)
		{
			::lua_rawgeti(luaState, tableIndex, index);
		}

		void LuaDLL::lua_rawseti(lua_State *luaState, int tableIndex, int index)
		{
			::lua_rawseti(luaState, tableIndex, index);
		}


		void *LuaDLL::lua_newuserdata(lua_State *luaState, int size)
		{
			return ::lua_newuserdata(luaState, size);
		}


		void *LuaDLL::lua_touserdata(lua_State *luaState, int index)
		{
			return ::lua_touserdata(luaState, index);
		}

#undef lua_getref

		void LuaDLL::lua_getref(lua_State *luaState, int reference)
		{
			lua_rawgeti(luaState, (int) LuaIndexes::LUA_REGISTRYINDEX,reference);
		}

		// Unwrapped
		// void luaL_unref(lua_State *luaState, int registryIndex, int reference);

#undef lua_unref

		void LuaDLL::lua_unref(lua_State *luaState, int reference)
		{
			::luaL_unref(luaState, (int) LuaIndexes::LUA_REGISTRYINDEX,reference);
		}

		bool LuaDLL::lua_isstring(lua_State *luaState, int index)
		{
			return ::lua_isstring(luaState, index) != 0;
		}


		bool LuaDLL::lua_iscfunction(lua_State *luaState, int index)
		{
			return ::lua_iscfunction(luaState, index) != 0;
		}

		void LuaDLL::lua_pushnil(lua_State *luaState)
		{
			::lua_pushnil(luaState);
		}



		void LuaDLL::lua_call(lua_State *luaState, int nArgs, int nResults)
		{
			::lua_call(luaState, nArgs, nResults);
		}

		int LuaDLL::lua_pcall(lua_State *luaState, int nArgs, int nResults, int errfunc)
		{			
			return ::lua_pcall(luaState, nArgs, nResults, errfunc);
		}

		// static int lua_rawcall(lua_State *luaState, int nArgs, int nResults)

        /*
		lua_CFunction LuaDLL::lua_tocfunction(lua_State *luaState, int index)
		{
			return ::lua_tocfunction(luaState, index);
		}
        */

		double LuaDLL::lua_tonumber(lua_State *luaState, int index)
		{
			return ::lua_tonumber(luaState, index);
		}


		bool LuaDLL::lua_toboolean(lua_State *luaState, int index)
		{
			return ::lua_toboolean(luaState, index);
		}

		// unwrapped
		// was out strLen
		// lua_State *lua_tolstring(lua_State *luaState, int index, [Out] int ^ strLen);

#undef lua_tostring

        std::string LuaDLL::lua_tostring(lua_State *luaState, int index)
		{
#if 1
			// FIXME use the same format string as lua i.e. LUA_NUMBER_FMT
            LuaTypes t = LuaDLL::lua_type(luaState,index);
			
			if(t == LuaTypes::LUA_TNUMBER)
                return std::to_string(LuaDLL::lua_tonumber(luaState, index));
			else if(t == LuaTypes::LUA_TSTRING)
			{
				size_t strlen;

                return std::string(::lua_tolstring(luaState, index, &strlen));
			}
			else if(t == LuaTypes::LUA_TNIL)
				return nullptr;			// treat lua nulls to as C# nulls
			else
				return "0";	// Because luaV_tostring does this
#else
			

			size_t strlen;

			// Note!  This method will _change_ the representation of the object on the stack to a string.
			// We do not want this behavior so we do the conversion ourselves
			const char *str = ::lua_tolstring(luaState, index, &strlen);
            if (str)
				return Marshal::PtrToStringAnsi(IntPtr((char *) str), strlen);
            else
                return nullptr;            // treat lua nulls to as C# nulls
#endif
		}

        int LuaDLL::__panic_cb(lua_State *l) {
            return panic_cb->runCallback(l);
        }
    
    
        void LuaDLL::lua_atpanic(lua_State *luaState, LuaCallback *panicf)
		{
            panic_cb = panicf;
			::lua_atpanic(luaState, (lua_CFunction) panic_cb);
		}
    
    
        int LuaDLL::__csCallbackWrapper(lua_State *l) {
            LuaCallback *fn = (LuaCallback*)::lua_touserdata(l, lua_upvalueindex(1));
            return fn->runCallback(l);
        }
    
    
		// lsthiros 1/15/2018 - leave stdcall and marshalling to SWIG.
    void LuaDLL::lua_pushstdcallcfunction(lua_State *luaState, LuaCallback *function)
		{
            lua_pushlightuserdata(luaState, function);
            lua_pushcclosure(luaState, &__csCallbackWrapper, 1);
		}
    

    void LuaDLL::lua_pushnumber(lua_State *luaState, double number)
		{
			::lua_pushnumber(luaState, number);
		}

    void LuaDLL::lua_pushboolean(lua_State *luaState, bool value)
		{
			::lua_pushboolean(luaState, value);
		}


    void LuaDLL::lua_pushstring(lua_State *luaState, const std::string &str)
		{
			const char *cs = str.c_str();
			::lua_pushstring(luaState, cs);
		}


    int LuaDLL::luaL_newmetatable(lua_State *luaState, const std::string &meta)
		{
			const char *cs = meta.c_str();
			int result = ::luaL_newmetatable(luaState, cs);
            
			return result;
		}


		// steffenj: BEGIN Lua 5.1.1 API change (luaL_getmetatable is now a macro using lua_getfield)
    void LuaDLL::lua_getfield(lua_State *luaState, int stackPos, const std::string &meta)
		{
			const char *cs = meta.c_str();
			::lua_getfield(luaState, stackPos, cs);
		}

#undef luaL_getmetatable

    void LuaDLL::luaL_getmetatable(lua_State *luaState, const std::string &meta)
		{
			lua_getfield(luaState, (int) LuaIndexes::LUA_REGISTRYINDEX, meta);
		}

    void *LuaDLL::luaL_checkudata(lua_State *luaState, int stackPos, const std::string &meta)
		{
			const char *cs = meta.c_str();

			void *result = ::luaL_checkudata(luaState, stackPos, cs);

			return result;
		}

    bool LuaDLL::luaL_getmetafield(lua_State *luaState, int stackPos, const std::string &field)
		{
            const char *cs = field.c_str();
			int result = ::luaL_getmetafield(luaState, stackPos, cs);

			return result != 0;
		}

		// wrapper not yet implemented
		// static int lua_load(lua_State *luaState, LuaChunkReader chunkReader, ref ReaderInfo data, std::string chunkName);

		int LuaDLL::luaL_loadbuffer(lua_State *luaState, const std::string &buff, const std::string &name)
		{
			// lsthiros 1/19/2018 - relying on SWIG string typemapping to simplify character
            // buffer extraction.
			return ::luaL_loadbuffer(luaState, buff.c_str(),buff.length(), name.c_str());
		}

		int LuaDLL::luaL_loadfile(lua_State *luaState, const std::string &filename)
		{
			const char *cs = filename.c_str();
			int result = ::luaL_loadfile(luaState, cs);

			return result;
		}

		void LuaDLL::lua_error(lua_State *luaState)
		{
			::lua_error(luaState);
		}


		bool LuaDLL::lua_checkstack(lua_State *luaState,int extra)
		{
			return ::lua_checkstack(luaState, extra) != 0;
		}


		int LuaDLL::lua_next(lua_State *luaState,int index)
		{
			return ::lua_next(luaState, index);
		}




		void LuaDLL::lua_pushlightuserdata(lua_State *luaState, void *udata)
		{
			::lua_pushlightuserdata(luaState, udata);
		}

		int LuaDLL::luanet_rawnetobj(lua_State *luaState,int obj)
		{
            int *udata= (int *) LuaDLL::lua_touserdata(luaState, obj);
			if(udata!=NULL) return *udata;
			return -1;
		}


      // lua debug hook functions added by Reinhard Ostermeier
        int LuaDLL::__lua_hookback(lua_State *l, lua_State *debug) {
            return hookBack->runHook(l, debug);
        }
        
        
      int LuaDLL::lua_sethook(lua_State *luaState, LuaHook *hook, int mask, int count)
      {
         return ::lua_sethook(luaState, (lua_Hook)__lua_hookback, mask, count);
      }

      int LuaDLL::lua_gethookmask(lua_State *luaState)
      {
         return ::lua_gethookmask(luaState);
      }

      int LuaDLL::lua_gethookcount(lua_State *luaState)
      {
         return ::lua_gethookcount(luaState);
      }

      int LuaDLL::lua_getstack(lua_State *luaState, int level, lua_State *luaDebug)
      {
         return ::lua_getstack(luaState, level, (lua_Debug*)luaDebug);
      }

      int LuaDLL::lua_getinfo(lua_State *luaState, const std::string &what, lua_State *luaDebug)
      {
          const char *cs = (char *) what.c_str();
         int ret = ::lua_getinfo(luaState, cs, (lua_Debug*)luaDebug);
         return ret;
      }

     std::string LuaDLL::lua_getlocal(lua_State *luaState, lua_State *luaDebug, int n)
     {
         const char* str = ::lua_getlocal(luaState, (lua_Debug*)luaDebug, n);
         if (str == NULL)
         {
            return nullptr;
         }
          return std::string(str);
      }

      std::string LuaDLL::lua_setlocal(lua_State *luaState, lua_State *luaDebug, int n)
      {
         const char* str = ::lua_setlocal(luaState, (lua_Debug*)luaDebug, n);
         if(str == NULL)
         {
            return nullptr;
         }
         return std::string(str);
      }

      std::string LuaDLL::lua_getupvalue(lua_State *luaState, int funcindex, int n)
      {
         const char* str = ::lua_getupvalue(luaState, funcindex, n);
         if(str == NULL)
         {
            return nullptr;
         }
          return std::string(str);
      }

      std::string LuaDLL::lua_setupvalue(lua_State *luaState, int funcindex, int n)
      {
         const char* str = ::lua_setupvalue(luaState, funcindex, n);
         if(str == NULL)
         {
            return nullptr;
         }
         
          return std::string(str);
      }
    
		// Starting with 5.1 the auxlib version of checkudata throws an exception if the type isn't right
		// Instead, we want to run our own version that checks the type and just returns null for failure
       void *LuaDLL::checkudata_raw(lua_State *L, int ud, const char *tname)
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
    
    
        int LuaDLL::luanet_checkudata(lua_State *luaState, int ud, const std::string &tname)
		{
			const char *cs = tname.c_str();
		    int *udata=(int*) checkudata_raw(luaState, ud, cs);

		    if(udata!=NULL) return *udata;
		    return -1;
		}


		bool LuaDLL::luaL_checkmetatable(lua_State *luaState,int index)
		{
			int retVal=0;

			if(lua_getmetatable(luaState,index)!=0) 
			{
				lua_pushlightuserdata(luaState, &tag);
				lua_rawget(luaState, -2);
				retVal = !lua_isnil(luaState, -1);
				lua_settop(luaState, -3);
			}
			return retVal;
		}

		int *LuaDLL::luanet_gettag()
		{
			return &tag;
		}

		void LuaDLL::luanet_newudata(lua_State *luaState,int val)
		{
			int* pointer=(int*) lua_newuserdata(luaState, sizeof(int));
			*pointer=val;
		}

		int LuaDLL::luanet_tonetobject(lua_State *luaState,int index)
		{
			int *udata;

			if(lua_type(luaState,index)==LuaTypes::LUA_TUSERDATA) 
			{
				if(luaL_checkmetatable(luaState, index)) 
				{
				udata=(int*) lua_touserdata(luaState,index);
				if(udata!=NULL) 
					return *udata; 
				}

			udata=(int*)checkudata_raw(luaState,index, "luaNet_class");
			if(udata!=NULL) return *udata;
			udata=(int*)checkudata_raw(luaState,index, "luaNet_searchbase");
			if(udata!=NULL) return *udata;
			udata=(int*)checkudata_raw(luaState,index, "luaNet_function");
			if(udata!=NULL) return *udata;
			}
			return -1;
		}
}
