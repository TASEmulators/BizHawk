using System;

namespace EMU7800.Core
{
    public class Emu7800SerializationException : Emu7800Exception
    {
        private Emu7800SerializationException()
        {
        }

        internal Emu7800SerializationException(string message) : base(message)
        {
        }

        internal Emu7800SerializationException(string message, Exception ex) : base(message, ex)
        {
        }
    }
}
