using System;
namespace LuaInterface
{
    public delegate void LuaHookFunction(IntPtr luaState, IntPtr luaDebug);
    public class LuaHookCaller: LuaHook
    {
        private LuaHookFunction function;

        public LuaHookCaller(LuaHookFunction function)
        {
            this.function = function;
        }

        public override int runHook(IntPtr l, IntPtr debug)
        {
            function(l, debug);
            return 0;
        }
    }
}
