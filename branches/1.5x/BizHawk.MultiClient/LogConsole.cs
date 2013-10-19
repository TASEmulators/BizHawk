using System;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

#pragma warning disable 162

//thanks! - http://sharp-developer.net/ru/CodeBank/WinForms/GuiConsole.aspx

//todo - quit using Console.WriteLine (well, we can leave it hooked up as a backstop)
//use a different method instead, so we can collect unicode data
//also, collect log data independently of whether the log window is open
//we also need to dice it into lines so that we can have a backlog policy

namespace BizHawk.MultiClient
{
	static class LogConsole
	{
		public static bool ConsoleVisible
		{
			get;
			private set;
		}

		static LogWindow window;
		static LogStream logStream;
		static bool NeedToRelease;

		class LogStream : Stream
		{
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
				string str = Encoding.ASCII.GetString(buffer, offset, count);
				if (Emit != null)
					Emit(str);
			}

			public Action<string> Emit;
		}


		static string SkipEverythingButProgramInCommandLine(string cmdLine)
		{
			//skip past the program name. can anyone think of a better way to do this?
			//we could use CommandLineToArgvW (commented out below) but then we would just have to re-assemble and potentially re-quote it
			int childCmdLine = 0;
			int lastSlash = 0;
			int lastGood = 0;
			bool quote = false;
			for (; ; )
			{
				char cur = cmdLine[childCmdLine];
				childCmdLine++;
				if (childCmdLine == cmdLine.Length) break;
				bool thisIsQuote = (cur == '\"');
				if (cur == '\\' || cur == '/')
					lastSlash = childCmdLine;
				if (quote)
				{
					if (thisIsQuote)
						quote = false;
					else lastGood = childCmdLine;
				}
				else
				{
					if (cur == ' ' || cur == '\t')
						break;
					if (thisIsQuote)
						quote = true;
					lastGood = childCmdLine;
				}
			}
			string remainder = cmdLine.Substring(childCmdLine);
			string path = cmdLine.Substring(lastSlash, lastGood - lastSlash);
			return path + " " + remainder;
		}

		static IntPtr oldOut, conOut;
		static bool hasConsole;
		static bool attachedConsole;
		static bool shouldRedirectStdout;
		public static void CreateConsole()
		{
			//(see desmume for the basis of some of this logic)

			if (hasConsole)
				return;

			if (oldOut == IntPtr.Zero)
				oldOut = Win32.GetStdHandle( -11 ); //STD_OUTPUT_HANDLE

			Win32.FileType fileType = Win32.GetFileType(oldOut);

			//stdout is already connected to something. keep using it and dont let the console interfere
			shouldRedirectStdout = (fileType == Win32.FileType.FileTypeUnknown || fileType == Win32.FileType.FileTypePipe);

			//attach to an existing console
			attachedConsole = false;

			//ever since a recent KB, XP-based systems glitch out when attachconsole is called and theres no console to attach to.
			if (Environment.OSVersion.Version.Major != 5)
			{
				if (Win32.AttachConsole(-1))
				{
				  hasConsole = true;
				  attachedConsole = true;
				}
			}

			if (!attachedConsole)
			{
				Win32.FreeConsole();
				if (Win32.AllocConsole())
				{
					//set icons for the console so we can tell them apart from the main window
					Win32.SendMessage(Win32.GetConsoleWindow(), 0x0080/*WM_SETICON*/, 0/*ICON_SMALL*/, Properties.Resources.console16x16.GetHicon().ToInt32());
					Win32.SendMessage(Win32.GetConsoleWindow(), 0x0080/*WM_SETICON*/, 1/*ICON_LARGE*/, Properties.Resources.console32x32.GetHicon().ToInt32());
					hasConsole = true;
				}
				else
					System.Windows.Forms.MessageBox.Show(string.Format("Couldn't allocate win32 console: {0}", Marshal.GetLastWin32Error()));
			}

			if(hasConsole)
			{
				IntPtr ptr = Win32.GetCommandLine();
				string commandLine = Marshal.PtrToStringAuto(ptr);
				Console.Title = SkipEverythingButProgramInCommandLine(commandLine);
			}

			if (shouldRedirectStdout)
			{
				conOut = Win32.CreateFile("CONOUT$", 0x40000000, 2, IntPtr.Zero, 3, 0, IntPtr.Zero);

				if (!Win32.SetStdHandle(-11, conOut))
				  throw new Exception("SetStdHandle() failed");
			}

			//DotNetRewireConout();
			hasConsole = true;

			if (attachedConsole)
			{
				Console.WriteLine();
				Console.WriteLine("use cmd /c {0} to get more sensible console behaviour", Path.GetFileName(PathManager.GetBasePathAbsolute()));
			}
		}

		static void ReleaseConsole()
		{
			if (!hasConsole)
				return;

			if(shouldRedirectStdout) Win32.CloseHandle(conOut);
			if(!attachedConsole) Win32.FreeConsole();
			Win32.SetStdHandle(-11, oldOut);

			conOut = IntPtr.Zero;
			hasConsole = false;
		}

		/// <summary>
		/// pops the console in front of the main window (where it should probably go after booting up the game).
		/// maybe this should be optional, or maybe we can somehow position the console sensibly.
		/// sometimes it annoys me, but i really need it on top while debugging or else i will be annoyed.
		/// best of all would be to position it beneath the bizhawk main window somehow.
		/// </summary>
		public static void PositionConsole()
		{
			if (ConsoleVisible == false) return;
			if (Global.Config.WIN32_CONSOLE)
			{
				IntPtr x = Win32.GetConsoleWindow();
				Win32.SetForegroundWindow(x);
			}
		}

		public static void ShowConsole()
		{
			if (ConsoleVisible) return;
			ConsoleVisible = true;

			if (Global.Config.WIN32_CONSOLE)
			{
				NeedToRelease = true;
				CreateConsole();
				//not sure whether we need to set a buffer size here
				//var sout = new StreamWriter(Console.OpenStandardOutput(),Encoding.ASCII,1) { AutoFlush = true };
				//var sout = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
				//Console.SetOut(sout);
				//Console.Title = "BizHawk Message Log";
				//System.Runtime.InteropServices.SafeFi
				//new Microsoft.Win32.SafeHandles.SafeFileHandle(
			}
			else
			{
				logStream = new LogStream();
				Log.HACK_LOG_STREAM = logStream;
				var sout = new StreamWriter(logStream) { AutoFlush = true };
				new StringBuilder(); //not using this right now
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
			if (NeedToRelease)
			{
				ReleaseConsole();
				NeedToRelease = false;
			}
			else
			{
				logStream.Close();
				logStream = null;
				Log.HACK_LOG_STREAM = null;
				window.Close();
				window = null;
			}
		}

		public static void notifyLogWindowClosing()
		{
			Console.SetOut(TextWriter.Null);
			ConsoleVisible = false;
			if(logStream != null) logStream.Close();
			Log.HACK_LOG_STREAM = null;
		}

		public static void SaveConfigSettings()
		{
			if (window != null && window.IsHandleCreated)
				window.SaveConfigSettings();
		}
	}
}
