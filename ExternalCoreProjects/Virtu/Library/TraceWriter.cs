using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;

namespace Jellyfish.Virtu.Services
{
	internal static class TraceWriter
	{
		public static void Write(string format, params object[] args)
		{
			Trace.WriteLine(FormatMessage(format, args));
		}

		private static string FormatMessage(string format, params object[] args)
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
					Write("[DebugService.FormatMessage] format: {0}; args: {1}; exception: {2}", format, string.Join(", ", args), ex.Message);
				}
			}
			else
			{
				message.Append(format);
			}

			return message.ToString();
		}
	}
}
