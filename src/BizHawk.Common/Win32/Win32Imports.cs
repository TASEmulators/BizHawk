﻿#nullable disable

using System.IO;
using System.Runtime.InteropServices;

// ReSharper disable UnusedMember.Global

#pragma warning disable CA1069 // This warning is just dumb

namespace BizHawk.Common
{
	/// <summary>
	/// This is more just an assorted bunch of Win32 functions
	/// </summary>
	public static class Win32Imports
	{
		public const int MAX_PATH = 260;

		[Flags]
		public enum TPM
		{
			LEFTBUTTON = 0x0000,
			RIGHTBUTTON = 0x0002,
			LEFTALIGN = 0x0000,
			CENTERALIGN = 0x000,
			RIGHTALIGN = 0x000,
			TOPALIGN = 0x0000,
			VCENTERALIGN = 0x0010,
			BOTTOMALIGN = 0x0020,
			HORIZONTAL = 0x0000,
			VERTICAL = 0x0040,
			NONOTIFY = 0x0080,
			RETURNCMD = 0x0100,
			RECURSE = 0x0001,
			HORPOSANIMATION = 0x0400,
			HORNEGANIMATION = 0x0800,
			VERPOSANIMATION = 0x1000,
			VERNEGANIMATION = 0x2000,
			NOANIMATION = 0x4000,
			LAYOUTRTL = 0x8000,
		}

		[DllImport("user32.dll", ExactSpelling = true)]
		public static extern IntPtr CreatePopupMenu();

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		public static extern bool DeleteFileW(string lpFileName);

		[DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DestroyMenu(IntPtr hMenu);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
		public static extern unsafe int FormatMessageW(int flags, IntPtr source, uint messageId, uint languageId, char* outMsg, int size, IntPtr args);

		[DllImport("kernel32.dll", ExactSpelling = true)]
		public static extern uint GetLastError();

		[DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
		public static extern bool PathRelativePathToW([Out] char[] pszPath, [In] string pszFrom, [In] FileAttributes dwAttrFrom, [In] string pszTo, [In] FileAttributes dwAttrTo);

		[DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
		public static extern bool SystemParametersInfoW(int uAction, int uParam, ref int lpvParam, int flags);

		[DllImport("winmm.dll", ExactSpelling = true)]
		public static extern uint timeBeginPeriod(uint uMilliseconds);

		[DllImport("user32.dll", ExactSpelling = true)]
		public static extern int TrackPopupMenuEx(IntPtr hmenu, TPM fuFlags, int x, int y, IntPtr hwnd, IntPtr lptpm);

		[DllImport("kernel32.dll", ExactSpelling = true)]
		public static extern IntPtr GetCurrentProcess();

		[DllImport("kernel32.dll", ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsWow64Process(IntPtr hProcess, [MarshalAs(UnmanagedType.Bool)] out bool Wow64Process);
	}
}