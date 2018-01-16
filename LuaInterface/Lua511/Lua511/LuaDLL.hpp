//
//  LuaDLL.hpp
//  Lua511
//
//  Created by Louie Thiros on 1/15/18.
//  Copyright © 2018 BizHawk. All rights reserved.
//

#ifndef LuaDLL_h
#define LuaDLL_h

extern "C" {
    #include "lua.h"
}

#include <string>

namespace Lua511 {
    class LuaCallback {
    public:
        virtual ~LuaCallback() { }
        virtual int runCallback(lua_State *l) { return 0; }
    };
    
    class LuaHook {
    public:
        virtual ~LuaHook() { }
        virtual int runHook(lua_State *l, lua_State *debug) { return 0; }
    };

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
    typedef enum LuaTypes
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
    } LuaTypes;
    
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
    typedef enum LuaGCOptions
    {
        LUA_GCSTOP = 0,
        LUA_GCRESTART = 1,
        LUA_GCCOLLECT = 2,
        LUA_GCCOUNT = 3,
        LUA_GCCOUNTB = 4,
        LUA_GCSTEP = 5,
        LUA_GCSETPAUSE = 6,
        LUA_GCSETSTEPMUL = 7,
    } LuaGCOptions;

    class LuaDLL
    {
    public:
        static int lua_gc(lua_State *luaState, LuaGCOptions what, int data);
        static std::string lua_typename(lua_State *luaState, LuaTypes type);
#undef luaL_typename
        static std::string luaL_typename(lua_State *luaState, int stackPos);
        static void luaL_error(lua_State *luaState, std::string *message);
        static void luaL_where(lua_State *luaState, int level);
        static lua_State *luaL_newstate();
        static void lua_close(lua_State *luaState);
        static void luaL_openlibs(lua_State *luaState);
        static int luaL_loadstring(lua_State *luaState, std::string *chunk);
#undef luaL_dostring
        static int luaL_dostring(lua_State *luaState, std::string *chunk);
        static int lua_dostring(lua_State *luaState, std::string *chunk);
        static void lua_createtable(lua_State *luaState, int narr, int nrec);
#undef lua_newtable
        static void lua_newtable(lua_State *luaState);
        static int luaL_dofile(lua_State *luaState, std::string *fileName);
#undef lua_getglobal
        static void lua_getglobal(lua_State *luaState, std::string *name);
#undef lua_setglobal
        static void lua_setglobal(lua_State *luaState, std::string *name);
        static void lua_settop(lua_State *luaState, int newTop);
#undef lua_pop
        static void lua_pop(lua_State *luaState, int amount);
        static void lua_insert(lua_State *luaState, int newTop);
        static void lua_remove(lua_State *luaState, int index);
        static void lua_gettable(lua_State *luaState, int index);
        static void lua_rawget(lua_State *luaState, int index);
        static void lua_settable(lua_State *luaState, int index);
        static void lua_rawset(lua_State *luaState, int index);
        static lua_State *lua_newthread(lua_State *luaState);
        static int lua_resume(lua_State *luaState, int narg);
        static int lua_yield(lua_State *luaState, int nresults);
        static void lua_setmetatable(lua_State *luaState, int objIndex);
        static int lua_getmetatable(lua_State *luaState, int objIndex);
        static int lua_equal(lua_State *luaState, int index1, int index2);
        static void lua_pushvalue(lua_State *luaState, int index);
        static void lua_replace(lua_State *luaState, int index);
        static int lua_gettop(lua_State *luaState);
        static LuaTypes lua_type(lua_State *luaState, int index);
#undef lua_isnil
        static bool lua_isnil(lua_State *luaState, int index);
        static bool lua_isnumber(lua_State *luaState, int index);
#undef lua_isboolean
        static bool lua_isboolean(lua_State *luaState, int index);
        static int luaL_ref(lua_State *luaState, int registryIndex);
#undef lua_ref
        static int lua_ref(lua_State *luaState, int lockRef);
        static void lua_rawgeti(lua_State *luaState, int tableIndex, int index);
        static void lua_rawseti(lua_State *luaState, int tableIndex, int index);
        static void *lua_newuserdata(lua_State *luaState, int size);
        static void *lua_touserdata(lua_State *luaState, int index);
#undef lua_getref
        static void lua_getref(lua_State *luaState, int reference);
#undef lua_unref
        static void lua_unref(lua_State *luaState, int reference);
        static bool lua_isstring(lua_State *luaState, int index);
        static bool lua_iscfunction(lua_State *luaState, int index);
        static void lua_pushnil(lua_State *luaState);
        static void lua_call(lua_State *luaState, int nArgs, int nResults);
        static int lua_pcall(lua_State *luaState, int nArgs, int nResults, int errfunc);
        static lua_CFunction lua_tocfunction(lua_State *luaState, int index);
        static double lua_tonumber(lua_State *luaState, int index);
        static bool lua_toboolean(lua_State *luaState, int index);
#undef lua_tostring
        static std::string lua_tostring(lua_State *luaState, int index);
        static void lua_atpanic(lua_State *luaState, LuaCallback *panicf);
        static void lua_pushstdcallcfunction(lua_State *luaState, LuaCallback *function);
        static void lua_pushnumber(lua_State *luaState, double number);
        static void lua_pushboolean(lua_State *luaState, bool value);
        static void lua_pushstring(lua_State *luaState, std::string *str);
        static int luaL_newmetatable(lua_State *luaState, std::string *meta);
        static void lua_getfield(lua_State *luaState, int stackPos, std::string *meta);
#undef luaL_getmetatable
        static void luaL_getmetatable(lua_State *luaState, std::string *meta);
        static void *luaL_checkudata(lua_State *luaState, int stackPos, std::string *meta);
        static bool luaL_getmetafield(lua_State *luaState, int stackPos, std::string *field);
        static int luaL_loadbuffer(lua_State *luaState, std::string *buff, std::string *name);
        static int luaL_loadfile(lua_State *luaState, std::string *filename);
        static void lua_error(lua_State *luaState);
        static bool lua_checkstack(lua_State *luaState,int extra);
        static int lua_next(lua_State *luaState,int index);
        static void lua_pushlightuserdata(lua_State *luaState, void *udata);
        static int luanet_rawnetobj(lua_State *luaState,int obj);
        static int lua_sethook(lua_State *luaState, LuaHook *hook, int mask, int count);
        static int lua_gethookmask(lua_State *luaState);
        static int lua_gethookcount(lua_State *luaState);
        static int lua_getstack(lua_State *luaState, int level, lua_State *luaDebug);
        static int lua_getinfo(lua_State *luaState, std::string *what, lua_State *luaDebug);
        static std::string lua_getlocal(lua_State *luaState, lua_State *luaDebug, int n);
        static std::string lua_setlocal(lua_State *luaState, lua_State *luaDebug, int n);
        static std::string lua_getupvalue(lua_State *luaState, int funcindex, int n);
        static std::string lua_setupvalue(lua_State *luaState, int funcindex, int n);
        static int luanet_checkudata(lua_State *luaState, int ud, std::string *tname);
        static bool luaL_checkmetatable(lua_State *luaState,int index);
        static int *luanet_gettag();
        static void luanet_newudata(lua_State *luaState,int val);
        static int luanet_tonetobject(lua_State *luaState,int index);
    private:
        static int __panic_cb(lua_State *l);
        static int __lua_hookback(lua_State *l, lua_State *debug);
        static int __csCallbackWrapper(lua_State *l);
        static void *checkudata_raw(lua_State *L, int ud, const char *tname);
    };
}

#endif /* LuaDLL_h */
