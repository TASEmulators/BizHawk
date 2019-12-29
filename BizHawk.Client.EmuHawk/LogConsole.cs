using System;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using BizHawk.Common;
using BizHawk.Client.Common;

// thanks! - http://sharp-developer.net/ru/CodeBank/WinForms/GuiConsole.aspx
// todo - quit using Console.WriteLine (well, we can leave it hooked up as a backstop)
// use a different method instead, so we can collect unicode data
// also, collect log data independently of whether the log window is open
// we also need to dice it into lines so that we can have a backlog policy

namespace BizHawk.Client.EmuHawk
{
	internal static class LogConsole
	{
		public static bool ConsoleVisible { get; private set; }

		private static LogWindow _window;
		private static LogStream _logStream;
		private static bool _needToRelease;

		private class LogStream : Stream
		{
			public override bool CanRead => false;
			public override bool CanSeek => false;
			public override bool CanWrite => true;

			public override void Flush()
			{
				//TODO - maybe this will help with decoding
			}

			public override long Length => throw new NotImplementedException();

			public override long Position
			{
				get => throw new NotImplementedException();
				set => throw new NotImplementedException();
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
				// TODO - buffer undecoded characters (this may be important)
				//(use decoder = System.Text.Encoding.Unicode.GetDecoder())
				string str = Encoding.ASCII.GetString(buffer, offset, count);
				Emit?.Invoke(str);
			}

			public Action<string> Emit;
		}

		internal static string SkipEverythingButProgramInCommandLine(string cmdLine)
		{
			// skip past the program name. can anyone think of a better way to do this?
			// we could use CommandLineToArgvW (commented out below) but then we would just have to re-assemble and potentially re-quote it
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
				{
					lastSlash = childCmdLine;
				}

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
			return $"{path} {remainder}";
		}

		private static IntPtr _oldOut, _conOut;
		private static bool _hasConsole;
		private static bool _attachedConsole;
		private static bool _shouldRedirectStdout;
		public static void CreateConsole()
		{
			// (see desmume for the basis of some of this logic)
			if (_hasConsole)
			{
				return;
			}

			if (_oldOut == IntPtr.Zero)
			{
				_oldOut = ConsoleImports.GetStdHandle( -11 ); // STD_OUTPUT_HANDLE
			}

			var fileType = ConsoleImports.GetFileType(_oldOut);

			// stdout is already connected to something. keep using it and don't let the console interfere
			_shouldRedirectStdout = (fileType == ConsoleImports.FileType.FileTypeUnknown || fileType == ConsoleImports.FileType.FileTypePipe);

			// attach to an existing console
			_attachedConsole = false;

			// ever since a recent KB, XP-based systems glitch out when AttachConsole is called and there's no console to attach to.
			if (Environment.OSVersion.Version.Major != 5)
			{
				if (ConsoleImports.AttachConsole(-1))
				{
				  _hasConsole = true;
				  _attachedConsole = true;
				}
			}

			if (!_attachedConsole)
			{
				ConsoleImports.FreeConsole();
				if (ConsoleImports.AllocConsole())
				{
					//set icons for the console so we can tell them apart from the main window
					Win32Imports.SendMessage(ConsoleImports.GetConsoleWindow(), 0x0080/*WM_SETICON*/, (IntPtr)0/*ICON_SMALL*/, Properties.Resources.console16x16.GetHicon());
					Win32Imports.SendMessage(ConsoleImports.GetConsoleWindow(), 0x0080/*WM_SETICON*/, (IntPtr)1/*ICON_LARGE*/, Properties.Resources.console32x32.GetHicon());
					_hasConsole = true;
				}
				else
				{
					MessageBox.Show($"Couldn't allocate win32 console: {Marshal.GetLastWin32Error()}");
				}
			}

			if (_hasConsole)
			{
				IntPtr ptr = ConsoleImports.GetCommandLine();
				string commandLine = Marshal.PtrToStringAuto(ptr);
				Console.Title = SkipEverythingButProgramInCommandLine(commandLine);
			}

			if (_shouldRedirectStdout)
			{
				_conOut = ConsoleImports.CreateFile("CONOUT$", 0x40000000, 2, IntPtr.Zero, 3, 0, IntPtr.Zero);

				if (!ConsoleImports.SetStdHandle(-11, _conOut))
				  throw new Exception($"{nameof(ConsoleImports.SetStdHandle)}() failed");
			}

			//DotNetRewireConout();
			_hasConsole = true;

			if (_attachedConsole)
			{
				Console.WriteLine();
				Console.WriteLine("use cmd /c {0} to get more sensible console behaviour", Path.GetFileName(PathManager.GetGlobalBasePathAbsolute()));
			}
		}

		static void ReleaseConsole()
		{
			if (!_hasConsole)
			{
				return;
			}

			if (_shouldRedirectStdout)
			{
				ConsoleImports.CloseHandle(_conOut);
			}

			if (!_attachedConsole)
			{
				ConsoleImports.FreeConsole();
			}

			ConsoleImports.SetStdHandle(-11, _oldOut);

			_conOut = IntPtr.Zero;
			_hasConsole = false;
		}

		/// <summary>
		/// pops the console in front of the main window (where it should probably go after booting up the game).
		/// maybe this should be optional, or maybe we can somehow position the console sensibly.
		/// sometimes it annoys me, but i really need it on top while debugging or else i will be annoyed.
		/// best of all would be to position it beneath the BizHawk main window somehow.
		/// </summary>
		public static void PositionConsole()
		{
			if (ConsoleVisible == false)
			{
				return;
			}

			if (Global.Config.WIN32_CONSOLE)
			{
				IntPtr x = ConsoleImports.GetConsoleWindow();
				ConsoleImports.SetForegroundWindow(x);
			}
		}

		public static void ShowConsole(MainForm parent)
		{
			if (ConsoleVisible) return;
			ConsoleVisible = true;

			if (Global.Config.WIN32_CONSOLE)
			{
				_needToRelease = true;
				CreateConsole();
			}
			else
			{
				_logStream = new LogStream();
				Log.HACK_LOG_STREAM = _logStream;
				Console.SetOut(new StreamWriter(_logStream) { AutoFlush = true });
				_window = new LogWindow(parent);
				_window.Show();
				_logStream.Emit = str => { _window.Append(str); };
			}
		}

		public static void HideConsole()
		{
			if (ConsoleVisible == false)
			{
				return;
			}

			Console.SetOut(TextWriter.Null);
			ConsoleVisible = false;
			if (_needToRelease)
			{
				ReleaseConsole();
				_needToRelease = false;
			}
			else
			{
				_logStream.Close();
				_logStream = null;
				Log.HACK_LOG_STREAM = null;
				_window.Close();
				_window = null;
			}
		}

		public static void NotifyLogWindowClosing()
		{
			Console.SetOut(TextWriter.Null);
			ConsoleVisible = false;
			_logStream?.Close();
			Log.HACK_LOG_STREAM = null;
		}

		public static void SaveConfigSettings()
		{
			if (_window != null && _window.IsHandleCreated)
			{
				_window.SaveConfigSettings();
			}
		}
	}
}
