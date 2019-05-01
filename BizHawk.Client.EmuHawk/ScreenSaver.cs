using System;
using System.Runtime.InteropServices;

using BizHawk.Client.Common;
using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	// Derived from http://www.codeproject.com/KB/cs/ScreenSaverControl.aspx
	public static class ScreenSaver
	{
		private interface PlatformSpecificScreenBlankInterface
		{
			Int32 Get();
			void Set(Int32 v);
		}
		private class WinScreenBlankInterface : PlatformSpecificScreenBlankInterface
		{
			[DllImport("user32.dll", CharSet = CharSet.Auto)]
			private static extern bool SystemParametersInfo(int uAction, int uParam, ref int lpvParam, int flags);
			public Int32 Get()
			{
				Int32 value = 0;
				SystemParametersInfo(SPI_GETSCREENSAVERTIMEOUT, 0, ref value, 0);
				return value;
			}
			public void Set(Int32 v)
			{
				int nullVar = 0;
				SystemParametersInfo(SPI_SETSCREENSAVERTIMEOUT, v, ref nullVar, SPIF_SENDWININICHANGE);
			}
		}
		private class MiscUnixScreenBlankInterface : PlatformSpecificScreenBlankInterface
		{
			public Int32 Get()
			{
				return 0; //TODO implement
			}
			public void Set(Int32 v)
			{
				//TODO implement
			}
		}
		private static PlatformSpecificScreenBlankInterface screenBlankInterface = PlatformLinkedLibSingleton.CurrentOS == PlatformLinkedLibSingleton.DistinctOS.Windows
			? (PlatformSpecificScreenBlankInterface) new WinScreenBlankInterface()
			: new MiscUnixScreenBlankInterface();

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
			return screenBlankInterface.Get();
		}

		// Pass in the number of seconds to set the screen saver timeout value.
		private static void SetScreenSaverTimeout(Int32 Value)
		{
			screenBlankInterface.Set(Value);
		}
	}
}
