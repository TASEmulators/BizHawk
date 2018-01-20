using System;
using Xunit;
using LuaInterface;

namespace LuaInterfaceTest
{
    public class LuaDLLTest
    {
        [Fact]
        public void testConstruction()
        {
            IntPtr lua = LuaDLL.luaL_newstate();
            LuaDLL.lua_close(lua);
        }

        [Fact]
        public void testToNumberPush()
        {
            IntPtr lua = LuaDLL.luaL_newstate();
            LuaDLL.lua_pushnumber(lua, 0);
            LuaDLL.lua_pushnumber(lua, 1);
            LuaDLL.lua_pushnumber(lua, 2);

            Assert.Equal(2, LuaDLL.lua_tonumber(lua, -1));
            Assert.Equal(1, LuaDLL.lua_tonumber(lua, -2));
            Assert.Equal(0, LuaDLL.lua_tonumber(lua, -3));

            LuaDLL.lua_close(lua);
        }

        [Fact]
        public void testPushPop()
        {
            IntPtr lua = LuaDLL.luaL_newstate();
            LuaDLL.lua_pushnumber(lua, 0);
            LuaDLL.lua_pushnumber(lua, 1);
            LuaDLL.lua_pushnumber(lua, 2);

            Assert.Equal(2, LuaDLL.lua_tonumber(lua, -1));
            LuaDLL.lua_pop(lua, 1);
            Assert.Equal(1, LuaDLL.lua_tonumber(lua, -1));
            LuaDLL.lua_pop(lua, 1);
            Assert.Equal(0, LuaDLL.lua_tonumber(lua, -1));
            LuaDLL.lua_pop(lua, 1);

            LuaDLL.lua_close(lua);
        }

        [Fact]
        public void testCallback() {
            IntPtr lua = LuaDLL.luaL_newstate();

            bool wasCalled = false;

            LuaCSFunction cb = new LuaCSFunction((luaState) =>
            {
                wasCalled = true;
                return 0;
            });

            LuaCSCaller caller = new LuaCSCaller(cb);
            LuaDLL.lua_pushstdcallcfunction(lua, caller);
            LuaDLL.lua_call(lua, 0, 0);

            Assert.True(wasCalled);
            LuaDLL.lua_close(lua);
        }
    }
}
