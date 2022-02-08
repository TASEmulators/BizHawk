using System;
using System.Runtime.InteropServices;
using System.Text;

namespace BizHawk.Common
{
	/// <summary>Gets/Sets the current working directory while bypassing the security checks triggered by the public API (<see cref="Environment.CurrentDirectory"/>).</summary>
	public static unsafe class CWDHacks
	{
		private const uint BUFFER_LEN = 0x200U;

		private static readonly byte[] BUFFER = new byte[BUFFER_LEN];

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern uint GetCurrentDirectoryW(uint nBufferLength, byte* pBuffer);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool SetCurrentDirectoryW(byte* lpPathName);

		public static bool Set(string newCWD)
		{
			fixed (byte* pstr = &Encoding.Unicode.GetBytes($"{newCWD}\0")[0])
				return SetCurrentDirectoryW(pstr);
		}

		public static string Get()
		{
			uint result;
			fixed (byte* pBuf = &BUFFER[0]) result = GetCurrentDirectoryW(BUFFER_LEN, pBuf);
			if (result <= BUFFER_LEN && result is not 0U) return Encoding.Unicode.GetString(BUFFER, 0, (int) (2U * result));
			var buf = new byte[result];
			uint result1;
			fixed (byte* pBuf = &buf[0]) result1 = GetCurrentDirectoryW(BUFFER_LEN, pBuf);
			if (result1 == result) return Encoding.Unicode.GetString(buf, 0, (int) (2U * result));
			throw new Exception();
		}
	}
}
