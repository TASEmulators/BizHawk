using System;

namespace EMU7800.Core
{
    public class Emu7800Exception : Exception
    {
        internal Emu7800Exception()
        {
        }

        internal Emu7800Exception(string message) : base(message)
        {
        }

        internal Emu7800Exception(string message, Exception ex) : base(message, ex)
        {
        }
    }
}
