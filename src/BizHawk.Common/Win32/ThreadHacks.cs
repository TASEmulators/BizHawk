#nullable disable

using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

namespace BizHawk.Common
{
	/// <remarks>
	/// largely from https://raw.githubusercontent.com/noserati/tpl/master/ThreadAffinityTaskScheduler.cs (MIT license)<br/>
	/// most of this is used in <c>#if false</c> code in <c>mupen64plusApi.frame_advance()</c>, don't delete it
	/// </remarks>
	public static class ThreadHacks
	{
		public const uint QS_ALLINPUT = 0x4FFU;
		public const uint MWMO_INPUTAVAILABLE = 0x0004U;

		[DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
		public static extern uint MsgWaitForMultipleObjectsEx(uint nCount, IntPtr[] pHandles, uint dwMilliseconds, uint dwWakeMask, uint dwFlags);

		[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
		public static extern int WaitForSingleObject(SafeWaitHandle handle, uint milliseconds);
	}
}
