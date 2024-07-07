using System.Collections.Generic;
using System.IO;

namespace BizHawk.Common
{
	public static class Log
	{
		static Log()
		{
			// You can set current desired logging settings here.
			// Production builds should be done with all logging disabled.
			LogToConsole = true;
			//LogToFile = true;
			//LogFilename = "d:/bizhawk.log";
			//EnableDomain("CD");
			//EnableDomain("CPU");
			//EnableDomain("VDC");
			//EnableDomain("MEM");
		}

		// -------------- Logging Domain Configuration --------------
		private static readonly List<string> EnabledLogDomains = new List<string>();

		public static void EnableDomain(string domain)
		{
			if (!EnabledLogDomains.Contains(domain))
			{
				EnabledLogDomains.Add(domain);
			}
		}

		public static void DisableDomain(string domain)
		{
			if (EnabledLogDomains.Contains(domain))
			{
				EnabledLogDomains.Remove(domain);
			}
		}

		// -------------- Logging Action Configuration --------------
#pragma warning disable CA2211 // public field
		public static Action<string> LogAction = DefaultLogger;
#pragma warning restore CA2211

		// NOTEs are only logged if the domain is enabled.
		// ERRORs are logged regardless.

		public static void Note(string domain, string msg, params object[] vals)
		{
			if (EnabledLogDomains.Contains(domain))
			{
				LogAction(string.Format(msg, vals));
			}
		}

		public static void Error(string domain, string msg, params object[] vals)
		{
			LogAction(string.Format(msg, vals));
		}

		// -------------- Default Logger Action --------------
		private static readonly bool LogToConsole = false;
		private static readonly bool LogToFile = false;

		private const string LogFilename = "bizhawk.txt";
		private static StreamWriter? _writer;

		private static void DefaultLogger(string message)
		{
			if (LogToConsole)
			{
				Console.WriteLine(message);
			}

			if (LogToFile)
			{
				_writer ??= new StreamWriter(LogFilename);
				_writer.WriteLine(message);
				_writer.Flush();
			}
		}
	}
}
