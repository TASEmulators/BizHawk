using System;
using System.Runtime.InteropServices;

namespace BizHawk.MultiClient
{
	// Derived from http://www.codeproject.com/KB/cs/ScreenSaverControl.aspx
	public static class ScreenSaver
	{
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		private static extern bool SystemParametersInfo(int uAction, int uParam, ref int lpvParam, int flags);

		private const int SPI_GETSCREENSAVERTIMEOUT = 14;
		private const int SPI_SETSCREENSAVERTIMEOUT = 15;
		private const int SPIF_SENDWININICHANGE = 2;

		public static void ResetTimerImmediate()
		{
			SetScreenSaverTimeout(GetScreenSaverTimeout());
		}

		private static int ctr;
		public static void ResetTimerPeriodically()
		{
			ctr++;
			if (ctr == 120)
			{
				SetScreenSaverTimeout(GetScreenSaverTimeout());
				ctr = 0;
			}
		}

		// Returns the screen saver timeout setting, in seconds
		private static Int32 GetScreenSaverTimeout()
		{
			Int32 value = 0;
			SystemParametersInfo(SPI_GETSCREENSAVERTIMEOUT, 0, ref value, 0);
			return value;
		}

		// Pass in the number of seconds to set the screen saver timeout value.
		private static void SetScreenSaverTimeout(Int32 Value)
		{
			int nullVar = 0;
			SystemParametersInfo(SPI_SETSCREENSAVERTIMEOUT, Value, ref nullVar, SPIF_SENDWININICHANGE);
		}
	}
}
