#nullable disable

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

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		public static extern bool DeleteFileW(string lpFileName);

		[DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
		public static extern uint MapVirtualKeyW(uint uCode, uint uMapType);

		[DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
		public static extern bool PathRelativePathToW([Out] char[] pszPath, [In] string pszFrom, [In] FileAttributes dwAttrFrom, [In] string pszTo, [In] FileAttributes dwAttrTo);

		[DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
		public static extern bool SystemParametersInfoW(int uAction, int uParam, ref int lpvParam, int flags);

		[DllImport("winmm.dll", ExactSpelling = true)]
		public static extern uint timeBeginPeriod(uint uMilliseconds);
	}
}
