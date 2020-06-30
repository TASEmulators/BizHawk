using System;
using System.Runtime.InteropServices;
using BizHawk.BizInvoke;

namespace BizHawk.Emulation.Cores.Waterbox
{
	unsafe partial class Syscalls
	{
		internal const uint S_IFMT = 61440;

		internal const uint S_IFDIR = 16384;
		internal const uint S_IFCHR = 8192;
		internal const uint S_IFBLK = 24576;
		internal const uint S_IFREG = 32768;
		internal const uint S_IFIFO = 4096;
		internal const uint S_IFLNK = 40960;
		internal const uint S_IFSOCK = 49152;

		internal const uint S_ISUID = 2048;
		internal const uint S_ISGID = 1024;
		internal const uint S_ISVTX = 512;
		internal const uint S_IRUSR = 0400;
		internal const uint S_IWUSR = 256;
		internal const uint S_IXUSR = 64;
		internal const uint S_IRWXU = 448;
		internal const uint S_IRGRP = 32;
		internal const uint S_IWGRP = 16;
		internal const uint S_IXGRP = 8;
		internal const uint S_IRWXG = 56;
		internal const uint S_IROTH = 4;
		internal const uint S_IWOTH = 2;
		internal const uint S_IXOTH = 1;
		internal const uint S_IRWXO = 7;


		[StructLayout(LayoutKind.Sequential)]
		public struct KStat
		{
			public ulong st_dev;
			public ulong st_ino;
			public ulong st_nlink;

			public uint st_mode;
			public uint st_uid;
			public uint st_gid;
			public uint __pad0;
			public ulong st_rdev;
			public long st_size;
			public long st_blksize;
			public long st_blocks;

			public long st_atime_sec;
			public long st_atime_nsec;
			public long st_mtime_sec;
			public long st_mtime_nsec;
			public long st_ctime_sec;
			public long st_ctime_nsec;
			public long __unused0;
			public long __unused1;
			public long __unused2;
		}

		private void StatInternal(KStat* s, IFileObject o)
		{
			s->st_dev = 1;
			s->st_ino = 1;
			s->st_nlink = 0;

			uint flags = 0;
			if (o.Stream.CanRead)
				flags |= S_IRUSR | S_IRGRP | S_IROTH;
			if (o.Stream.CanWrite)
				flags |= S_IWUSR | S_IWGRP | S_IWOTH;
			if (o.Stream.CanSeek)
				flags |= S_IFREG;
			else
				flags |= S_IFIFO;
			s->st_mode = flags;
			s->st_uid = 0;
			s->st_gid = 0;
			s->__pad0 = 0;
			s->st_rdev = 0;
			if (o.Stream.CanSeek)
				s->st_size = o.Stream.Length;
			else
				s->st_size = 0;
			s->st_blksize = 4096;
			s->st_blocks = (s->st_size + 511) / 512;

			s->st_atime_sec = 1262304000000;
			s->st_atime_nsec = 1000000000 / 2;
			s->st_mtime_sec = 1262304000000;
			s->st_mtime_nsec = 1000000000 / 2;
			s->st_ctime_sec = 1262304000000;
			s->st_ctime_nsec = 1000000000 / 2;
		}

		[BizExport(CallingConvention.Cdecl, EntryPoint = "__wsyscalltab[4]")]
		public long Stat(string path, KStat* statbuf)
		{
			if (!_availableFiles.TryGetValue(path, out var o))
				return -ENOENT;

			StatInternal(statbuf, o);
			return 0;
		}

		[BizExport(CallingConvention.Cdecl, EntryPoint = "__wsyscalltab[5]")]
		public long Fstat(int fd, KStat* statbuf)
		{
			if (fd < 0 || fd >= _openFiles.Count)
				return -EBADF;
			var o = _openFiles[fd];
			if (o == null)
				return -EBADF;
			StatInternal(statbuf, o);
			return 0;
		}
	}
}
