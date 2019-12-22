using System;
using System.Runtime.InteropServices;

namespace BizHawk.Common
{
	public static class ConsoleImports
	{
		public enum FileType : uint
		{
			FileTypeUnknown = 0,
			FileTypeDisk = 1,
			FileTypeChar = 2,
			FileTypePipe = 3,
			FileTypeRemote = 0x8000
		}

		[DllImport("kernel32.dll")]
		public static extern FileType GetFileType(IntPtr hFile);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		public static extern IntPtr GetCommandLine();

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr GetConsoleWindow();

		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool SetForegroundWindow(IntPtr hWnd);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool AttachConsole(int dwProcessId);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool AllocConsole();

		[DllImport("kernel32.dll", SetLastError = false)]
		public static extern bool FreeConsole();

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr GetStdHandle(int nStdHandle);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool SetStdHandle(int nStdHandle, IntPtr hConsoleOutput);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr CreateFile(string fileName, int desiredAccess, int shareMode, IntPtr securityAttributes, int creationDisposition, int flagsAndAttributes, IntPtr templateFile);

		[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
		public static extern bool CloseHandle(IntPtr handle);
	}
}
