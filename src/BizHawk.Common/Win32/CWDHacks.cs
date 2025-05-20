using System.Runtime.InteropServices;

using Windows.Win32;

using static Windows.Win32.Win32Imports;

namespace BizHawk.Common
{
	/// <summary>Gets/Sets the current working directory while bypassing the security checks triggered by the public API (<see cref="Environment.CurrentDirectory"/>).</summary>
	public static class CWDHacks
	{
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
			Span<char> startingBuffer = stackalloc char[STARTING_BUF_SIZE];
			var ret = GetCurrentDirectoryW(startingBuffer);
			switch (ret)
			{
				case 0:
					throw GetExceptionForFailure();
				case < STARTING_BUF_SIZE: // ret should be smaller than the buffer, as ret doesn't include null terminator
					return startingBuffer.Slice(start: 0, length: unchecked((int) ret)).ToString();
			}

			// since current directory could suddenly grow (due to it being global / modifiable by other threads), a while true loop is used here
			// although it's fairly unlikely we'll even reach this point, MAX_PATH can only be bypassed under certain circumstances
			while (true)
			{
				var bufSize = ret;
				var buffer = new char[bufSize];
				ret = GetCurrentDirectoryW(buffer);
				if (ret == 0)
				{
					throw GetExceptionForFailure();
				}

				if (ret < bufSize)
				{
					return new(buffer, startIndex: 0, length: unchecked((int) ret));
				}
			}
		}
	}
}
