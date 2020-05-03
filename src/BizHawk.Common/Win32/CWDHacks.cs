using System;
using System.Runtime.InteropServices;

namespace BizHawk.Common
{
	/// <summary>Gets/Sets the current working directory while bypassing the security checks triggered by the public API (<see cref="Environment.CurrentDirectory"/>).</summary>
	public static unsafe class CWDHacks
	{
		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern uint GetCurrentDirectoryW(uint nBufferLength, byte* pBuffer);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool SetCurrentDirectoryW(byte* lpPathName);

		public static bool Set(string newCWD)
		{
			fixed (byte* pstr = &System.Text.Encoding.Unicode.GetBytes($"{newCWD}\0")[0])
				return SetCurrentDirectoryW(pstr);
		}

		public static string Get()
		{
			var buf = new byte[32768];
			fixed (byte* pBuf = &buf[0])
				return System.Text.Encoding.Unicode.GetString(buf, 0, 2 * (int) GetCurrentDirectoryW(32767, pBuf));
		}
	}
}
