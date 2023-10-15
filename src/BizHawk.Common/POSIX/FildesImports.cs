#nullable enable

using System;
using System.Runtime.InteropServices;

namespace BizHawk.Common
{
	public static class FildesImports
	{
		[Flags]
		public enum OpenFlags : int
		{
			O_RDONLY = 0,
			O_WRONLY = 1,
			O_RDWR = 2,
			// O_LARGEFILE = 0,
			O_CREAT = 0x40,
			O_EXCL = 0x80,
			O_NOCTTY = 0x100,
			O_TRUNC = 0x200,
			O_APPEND = 0x400,
			O_NONBLOCK = 0x800,
			O_NDELAY = O_NONBLOCK,
			O_DSYNC = 0x1000,
			O_ASYNC = 0x2000,
			O_DIRECT = 0x4000,
			O_DIRECTORY = 0x10000,
			O_NOFOLLOW = 0x20000,
			O_NOATIME = 0x40000,
			O_CLOEXEC = 0x80000,
			O_SYNC = 0x100000 | O_DSYNC,
			O_PATH = 0x200000,
			O_TMPFILE = 0x400000 | O_DIRECTORY,
		}

		[Flags]
		public enum OpenMode : int
		{
			S_IXOTH = 0x1,
			S_IWOTH = 0x2,
			S_IROTH = 0x4,
			S_IRWXO = S_IROTH | S_IWOTH | S_IXOTH,
			S_IXGRP = 0x8,
			S_IWGRP = 0x10,
			S_IRGRP = 0x20,
			S_IRWXG = S_IRGRP | S_IWGRP | S_IXGRP,
			S_IXUSR = 0x40,
			S_IWUSR = 0x80,
			S_IRUSR = 0x100,
			S_IRWXU = S_IRUSR | S_IWUSR | S_IXUSR,
			S_ISVTX = 0x200,
			S_ISGID = 0x400,
			S_ISUID = 0x800,
		}

		[DllImport("libc")]
		public static extern int open(string pathname, OpenFlags flags);

		[DllImport("libc")]
		public static extern int open(string pathname, OpenFlags flags, OpenMode mode);

		[DllImport("libc")]
		public static extern int close(int fd);

		[DllImport("libc", SetLastError = true)]
		public static extern unsafe IntPtr read(int fd, void* buf, IntPtr count);
	}
}
