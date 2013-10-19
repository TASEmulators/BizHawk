namespace EMU7800.Core
{
    public interface ILogger
    {
        void WriteLine(string format, params object[] args);
        void WriteLine(object value);
        void Write(string format, params object[] args);
        void Write(object value);
    }
}
