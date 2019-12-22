using System;
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
		public const uint PM_REMOVE = 0x0001U;

		[StructLayout(LayoutKind.Sequential)]
		public struct MSG
		{
			public IntPtr hwnd;
			public uint message;
			public IntPtr wParam;
			public IntPtr lParam;
			public uint time;
			public int x;
			public int y;
		}

		[DllImport("user32.dll")]
		public static extern IntPtr DispatchMessage([In] ref MSG lpMsg);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern uint MsgWaitForMultipleObjectsEx(uint nCount, IntPtr[] pHandles, uint dwMilliseconds, uint dwWakeMask, uint dwFlags);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool PeekMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool TranslateMessage([In] ref MSG lpMsg);

		[DllImport("kernel32", SetLastError = true, ExactSpelling = true)]
		public static extern int WaitForSingleObject(SafeWaitHandle handle, uint milliseconds);
	}
}
