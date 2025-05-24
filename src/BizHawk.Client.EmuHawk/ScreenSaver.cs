using BizHawk.Common;

using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

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
			public int Duration
			{
				get
				{
					_ = Win32Imports.SystemParametersInfoW(
						SYSTEM_PARAMETERS_INFO_ACTION.SPI_GETSCREENSAVETIMEOUT,
						uiParam: 0,
						out var value);
					return unchecked((int) value);
				}
				set
				{
					_ = Win32Imports.SystemParametersInfoW(
						SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETSCREENSAVETIMEOUT,
						unchecked((uint) value),
						SYSTEM_PARAMETERS_INFO_UPDATE_FLAGS.SPIF_SENDWININICHANGE);
				}
			}
		}

		private class UnixScreenBlankTimer : IScreenBlankTimer
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