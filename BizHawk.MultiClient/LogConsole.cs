using System;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

//todo - quit using Console.WriteLine (well, we can leave it hooked up as a backstop)
//use a different method instead, so we can collect unicode data
//also, collect log data independently of whether the log window is open
//we also need to dice it into lines so that we can have a backlog policy

namespace BizHawk.MultiClient
{
	static class LogConsole
	{
		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool AllocConsole();

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool FreeConsole();


		public static bool ConsoleVisible
		{
			get;
			private set;
		}

		static LogWindow window;
		static LogStream logStream;
		static StringBuilder sbLog;

		class LogStream : Stream
		{
			public LogStream()
			{
			}

			public override bool CanRead { get { return false; } }
			public override bool CanSeek { get { return false; } }
			public override bool CanWrite { get { return true; } }

			public override void Flush()
			{
				//TODO - maybe this will help with decoding
			}

			public override long Length
			{
				get { throw new NotImplementedException(); }
			}

			public override long Position
			{
				get
				{
					throw new NotImplementedException();
				}
				set
				{
					throw new NotImplementedException();
				}
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				throw new NotImplementedException();
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				throw new NotImplementedException();
			}

			public override void SetLength(long value)
			{
				throw new NotImplementedException();
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				//TODO - buffer undecoded characters (this may be important)
				//(use decoder = System.Text.Encoding.Unicode.GetDecoder())
				string str = System.Text.Encoding.ASCII.GetString(buffer, offset, count);
				if (Emit != null)
					Emit(str);
			}

			public Action<string> Emit;
		}

		const bool WIN32_CONSOLE = true;

		public static void ShowConsole()
		{
			if (ConsoleVisible) return;
			ConsoleVisible = true;

			if (WIN32_CONSOLE)
			{
				AllocConsole();
				var sout = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
				Console.SetOut(sout);
				Console.Title = "BizHawk Message Log";
			}
			else
			{
				logStream = new LogStream();
				Log.HACK_LOG_STREAM = logStream;
				var sout = new StreamWriter(logStream) { AutoFlush = true };
				sbLog = new StringBuilder(); //not using this right now
				Console.SetOut(sout);
				window = new LogWindow();
				window.Show();
				logStream.Emit = (str) => { window.Append(str); };
			}
		}

		public static void HideConsole()
		{
			if (ConsoleVisible == false) return;
			Console.SetOut(TextWriter.Null);
			ConsoleVisible = false;
			if (WIN32_CONSOLE)
			{
				FreeConsole();
			}
			else
			{
				window.Dispose();
				logStream.Dispose();
				logStream = null;
				Log.HACK_LOG_STREAM = null;
				window = null;
			}

		}
	}
}