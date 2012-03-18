using System;
using System.Collections.Generic;
using System.IO;

namespace BizHawk
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

        // ============== Logging Domain Configuration ==============
 
        private static List<string> EnabledLogDomains = new List<string>();
    
        public static void EnableDomain(string domain)
        {
            if (EnabledLogDomains.Contains(domain) == false)
                EnabledLogDomains.Add(domain);
        }

        public static void DisableDomain(string domain)
        {
            if (EnabledLogDomains.Contains(domain))
                EnabledLogDomains.Remove(domain);
        }

        // ============== Logging Action Configuration ==============

        public static Action<string> LogAction = DefaultLogger;

        // NOTEs are only logged if the domain is enabled.
        // ERRORs are logged regardless.

        public static void Note(string domain, string msg, params object[] vals)
        {
            if (EnabledLogDomains.Contains(domain))
                LogAction(String.Format(msg, vals));
        }

        public static void Error(string domain, string msg, params object[] vals)
        {
            LogAction(String.Format(msg, vals));
        }

        // ============== Default Logger Action ==============

		public static Stream HACK_LOG_STREAM;

        private static bool LogToConsole = false;
		private static bool LogToFile = false;

        private static string LogFilename = "bizhawk.txt";
        private static StreamWriter writer;

        private static void DefaultLogger(string message)
        {
            if (LogToConsole) 
                Console.WriteLine(message);

            if (LogToFile && writer == null) 
                writer = new StreamWriter(LogFilename);

            if (LogToFile)
            {
                writer.WriteLine(message);
                writer.Flush();
            }
        }
    }
}
