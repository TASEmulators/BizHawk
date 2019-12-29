using System;
using System.Text;
using System.IO;
using BizHawk.Common;

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

		private static bool _hasConsole;

		static void ReleaseConsole()
		{
			if (!_hasConsole)
			{
				return;
			}

			_hasConsole = false;
		}

		public static void ShowConsole(MainForm parent)
		{
			if (ConsoleVisible) return;
			ConsoleVisible = true;

			_logStream = new LogStream();
			Log.HACK_LOG_STREAM = _logStream;
			Console.SetOut(new StreamWriter(_logStream) { AutoFlush = true });
			_window = new LogWindow(parent);
			_window.Show();
			_logStream.Emit = str => { _window.Append(str); };
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
