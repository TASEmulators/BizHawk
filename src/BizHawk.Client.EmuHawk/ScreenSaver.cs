﻿using BizHawk.Common;

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

		private sealed class Win32ScreenBlankTimer : IScreenBlankTimer
		{
			public int Duration
			{
				get
				{
					const int SPI_GETSCREENSAVERTIMEOUT = 14;
					int value = default;
					Win32Imports.SystemParametersInfoW(SPI_GETSCREENSAVERTIMEOUT, 0, ref value, 0);
					return value;
				}
				set
				{
					const int SPI_SETSCREENSAVERTIMEOUT = 15;
					const int SPIF_SENDWININICHANGE = 2;
					int nullVar = default;
					Win32Imports.SystemParametersInfoW(SPI_SETSCREENSAVERTIMEOUT, value, ref nullVar, SPIF_SENDWININICHANGE);
				}
			}
		}

		private sealed class UnixScreenBlankTimer : IScreenBlankTimer
		{
			public int Duration { get; set; } = 0; //TODO implementation
		}

		private static readonly IScreenBlankTimer _screenBlankTimer = OSTailoredCode.IsUnixHost
			? new UnixScreenBlankTimer()
			: new Win32ScreenBlankTimer();

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
