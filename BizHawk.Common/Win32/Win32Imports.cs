using System;
using System.Runtime.InteropServices;

namespace BizHawk.Common
{
	public static class Win32Imports
	{
		public const int MAX_PATH = 260;

		[DllImport("kernel32.dll", EntryPoint = "DeleteFileW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
		public static extern bool DeleteFileW([MarshalAs(UnmanagedType.LPWStr)] string lpFileName);

		[DllImport("kernel32.dll", SetLastError=true)]
		public static extern unsafe uint GetCurrentDirectoryW(uint nBufferLength, byte* pBuffer);

		[DllImport("kernel32", SetLastError = true, EntryPoint = "GetProcAddress")]
		public static extern IntPtr GetProcAddressOrdinal(IntPtr hModule, IntPtr procName);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr GetProcessHeap();

		[DllImport("kernel32.dll", SetLastError = false)]
		public static extern IntPtr HeapAlloc(IntPtr hHeap, uint dwFlags, int dwBytes);

		/// <remarks>used in <c>#if false</c> code in <c>AviWriter.CodecToken.DeallocateAVICOMPRESSOPTIONS</c>, don't delete it</remarks>
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool HeapFree(IntPtr hHeap, uint dwFlags, IntPtr lpMem);

		[DllImport("user32")]
		public static extern bool HideCaret(IntPtr hWnd);

		[DllImport("kernel32.dll")]
		public static extern bool IsDebuggerPresent();

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern uint MapVirtualKey(uint uCode, uint uMapType);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern unsafe bool SetCurrentDirectoryW(byte* lpPathName);

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern bool SystemParametersInfo(int uAction, int uParam, ref int lpvParam, int flags);

		[DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
		public static extern uint timeBeginPeriod(uint uMilliseconds);
	}
}
