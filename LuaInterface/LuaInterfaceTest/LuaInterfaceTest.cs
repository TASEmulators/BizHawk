using System;
using Xunit;
using LuaInterface;

namespace LuaInterfaceTest
{
    public class LuaInterfaceTest
    {
        [Fact]
        public void testLuaConstruction() {
            Lua l = new Lua();
            l.Close();
        }

        [Fact]
        public void testLuaPush() {
            Lua l = new Lua();
            Object expected = new object();
            l.Push(expected);
            Object actual = l.Pop();
            Assert.Same(expected, actual);
            l.Close();
        }

        public delegate void TestCallback(int input);

        public class TestCallbacker
        {
            private TestCallback cb;
            public TestCallbacker(TestCallback cb)
            {
                this.cb = cb;
            }

            public void doCall(int input)
            {
                this.cb(input);
            }
        }

        [Fact]
        public void testRegisterFunction() {
            Lua l = new Lua();
            const int magic = 23;
            bool called = false;

            TestCallback cb = new TestCallback((int input) =>
            {
                if (input == magic)
                {
                    called = true;
                }
            });
            TestCallbacker cbr = new TestCallbacker(cb);
            l.RegisterFunction("doCall", cbr,
                               cbr.GetType().GetMethod("doCall"));
            l.DoString("doCall(23);");
            Assert.True(called);
            l.Close();
        }
    }
}
