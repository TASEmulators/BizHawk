using System;
namespace LuaInterface
{
    public delegate int LuaCSFunction(IntPtr luaState);

    public class LuaCSCaller: LuaCallback
    {
        private LuaCSFunction function;

        public LuaCSCaller(LuaCSFunction function)
        {
            this.function = function;
        }

        public override int runCallback(IntPtr l)
        {
            return function(l);
        }
    }
}
