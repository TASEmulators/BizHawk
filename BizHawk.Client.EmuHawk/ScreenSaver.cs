using System.Runtime.InteropServices;

using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	/// <remarks>Derived from http://www.codeproject.com/KB/cs/ScreenSaverControl.aspx</remarks>
	public static class ScreenSaver
	{
		private interface IScreenBlankTimer
		{
			/// <summary>
			/// The screen saver timeout setting, in seconds
			/// </summary>
			int Duration { get; set; }
		}

		private class Win32ScreenBlankTimer : IScreenBlankTimer
		{
			[DllImport("user32.dll", CharSet = CharSet.Auto)]
			private static extern bool SystemParametersInfo(int uAction, int uParam, ref int lpvParam, int flags);

			private const int SPI_GETSCREENSAVERTIMEOUT = 14;
			private const int SPI_SETSCREENSAVERTIMEOUT = 15;
			private const int SPIF_SENDWININICHANGE = 2;

			public int Duration
			{
				get
				{
					var value = 0;
					SystemParametersInfo(SPI_GETSCREENSAVERTIMEOUT, 0, ref value, 0);
					return value;
				}
				set
				{
					var nullVar = 0;
					SystemParametersInfo(SPI_SETSCREENSAVERTIMEOUT, value, ref nullVar, SPIF_SENDWININICHANGE);
				}
			}
		}

		private class UnixScreenBlankTimer : IScreenBlankTimer
		{
			public int Duration { get; set; } = 0; //TODO implementation
		}

		private static readonly IScreenBlankTimer _screenBlankTimer = OSTailoredCode.IsWindows()
			? (IScreenBlankTimer) new Win32ScreenBlankTimer()
			: new UnixScreenBlankTimer();

		private static int ctr;

		public static void ResetTimerImmediate()
		{
			_screenBlankTimer.Duration = _screenBlankTimer.Duration;
		}

		public static void ResetTimerPeriodically()
		{
			if (++ctr < 120) return;
			ctr = 0;
			ResetTimerImmediate();
		}
	}
}