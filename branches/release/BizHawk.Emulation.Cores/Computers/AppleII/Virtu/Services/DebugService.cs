using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;
using Jellyfish.Library;

namespace Jellyfish.Virtu.Services
{
    public class DebugService : MachineService
    {
        public DebugService(Machine machine) : 
            base(machine)
        {
        }

        public void WriteMessage(string message)
        {
            OnWriteMessage(FormatMessage(message));
        }

        public void WriteMessage(string format, params object[] args)
        {
            OnWriteMessage(FormatMessage(format, args));
        }

        protected virtual void OnWriteMessage(string message)
        {
#if SILVERLIGHT
            Debug.WriteLine(message);
#else
            Trace.WriteLine(message);
#endif
        }

        private string FormatMessage(string format, params object[] args)
        {
            var message = new StringBuilder(256);
            message.AppendFormat(CultureInfo.InvariantCulture, "[{0} T{1:X3} Virtu] ", DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture), Thread.CurrentThread.ManagedThreadId);
            if (args.Length > 0)
            {
                try
                {
                    message.AppendFormat(CultureInfo.InvariantCulture, format, args);
                }
                catch (FormatException ex)
                {
                    WriteMessage("[DebugService.FormatMessage] format: {0}; args: {1}; exception: {2}", format, string.Join(", ", args), ex.Message);
                }
            }
            else
            {
                message.Append(format);
            }

            return message.ToString();
        }

        public static DebugService Default { get { return _default.Value; } }

        private static readonly Lazy<DebugService> _default = new Lazy<DebugService>(() => new DebugService(null));
    }
}
