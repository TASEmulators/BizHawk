#nullable disable

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace BizHawk.Common
{
	/// <summary>
	/// This is more just an assorted bunch of Win32 functions
	/// </summary>
	public static class Win32Imports
	{
		public const int MAX_PATH = 260;

		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
		public static extern bool DeleteFileW([MarshalAs(UnmanagedType.LPWStr)] string lpFileName);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr GetModuleHandle(string lpModuleName);

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern uint MapVirtualKey(uint uCode, uint uMapType);

		[DllImport("shlwapi.dll", CharSet = CharSet.Auto)]
		public static extern bool PathRelativePathTo([Out] StringBuilder pszPath, [In] string pszFrom, [In] FileAttributes dwAttrFrom, [In] string pszTo, [In] FileAttributes dwAttrTo);

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern bool SystemParametersInfo(int uAction, int uParam, ref int lpvParam, int flags);

		[DllImport("winmm.dll")]
		public static extern uint timeBeginPeriod(uint uMilliseconds);
	}
}
