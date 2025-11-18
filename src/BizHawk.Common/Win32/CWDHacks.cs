using System.Runtime.InteropServices;

namespace BizHawk.Common
{
	/// <summary>Gets/Sets the current working directory while bypassing the security checks triggered by the public API (<see cref="Environment.CurrentDirectory"/>).</summary>
	public static class CWDHacks
	{
		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		private static extern unsafe int GetCurrentDirectoryW(int nBufferLength, char* lpBuffer);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		private static extern bool SetCurrentDirectoryW(string lpPathName);

		public static bool Set(string newCWD)
			=> SetCurrentDirectoryW(newCWD);

		public static unsafe string Get()
		{
			static Exception GetExceptionForFailure()
			{
				var ex = Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error())
					?? new("Marshal.GetExceptionForHR returned null?");
				return new InvalidOperationException("GetCurrentDirectoryW returned 0!", ex);
			}

			const int STARTING_BUF_SIZE = (int) Win32Imports.MAX_PATH + 1;
			var startingBuffer = stackalloc char[STARTING_BUF_SIZE];
			var ret = GetCurrentDirectoryW(STARTING_BUF_SIZE, startingBuffer);
			switch (ret)
			{
				case 0:
					throw GetExceptionForFailure();
				case < STARTING_BUF_SIZE: // ret should be smaller than the buffer, as ret doesn't include null terminator
					return new(startingBuffer, 0, ret);
			}

			// since current directory could suddenly grow (due to it being global / modifiable by other threads), a while true loop is used here
			// although it's fairly unlikely we'll even reach this point, MAX_PATH can only be bypassed under certain circumstances
			while (true)
			{
				var bufSize = ret;
				var buffer = new char[bufSize];
				fixed (char* p = buffer)
				{
					ret = GetCurrentDirectoryW(bufSize, p);
					if (ret == 0)
					{
						throw GetExceptionForFailure();
					}

					if (ret < bufSize)
					{
						return new(p, 0, ret);
					}
				}
			}
		}
	}
}
