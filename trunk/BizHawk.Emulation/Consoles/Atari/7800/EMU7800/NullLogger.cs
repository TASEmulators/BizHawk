namespace EMU7800.Core
{
    public class NullLogger : ILogger
    {
        public void WriteLine(string format, params object[] args)
        {
        }

        public void WriteLine(object value)
        {
        }

        public void Write(string format, params object[] args)
        {
        }

        public void Write(object value)
        {
        }
    }
}
