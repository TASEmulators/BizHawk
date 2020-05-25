using System;
using static BizHawk.BizInvoke.MemoryBlock;
using static BizHawk.BizInvoke.POSIXLibC;

namespace BizHawk.BizInvoke
{
	public sealed class MemoryBlockUnixPal : IMemoryBlockPal
	{
		/// <summary>handle returned by <see cref="memfd_create"/></summary>
		private int _fd = -1;
		private ulong _start;
		private ulong _size;

		/// <summary>
		/// Reserve bytes to later be swapped in, but do not map them
		/// </summary>
		/// <param name="start">eventual mapped address</param>
		/// <param name="size"></param>
		/// <exception cref="InvalidOperationException">failed to get file descriptor (never thrown as <see cref="NotImplementedException"/> is thrown first)</exception>
		/// <exception cref="NotImplementedException">always</exception>
		public MemoryBlockUnixPal(ulong start, ulong size)
		{
			_start = start;
			_size = size;
			throw new NotImplementedException($"{nameof(MemoryBlockUnixPal)} ctor");
			#if false
			_fd = memfd_create("MemoryBlockUnix", 0);
			if (_fd == -1)
				throw new InvalidOperationException($"{nameof(memfd_create)}() returned -1");
			#endif
		}

		public void Dispose()
		{
			if (_fd == -1)
				return;
			close(_fd);
			_fd = -1;
			GC.SuppressFinalize(this);
		}

		~MemoryBlockUnixPal()
		{
			Dispose();
		}

		public void Activate()
		{
			var ptr = mmap(Z.US(_start), Z.UU(_size), MemoryProtection.Read | MemoryProtection.Write | MemoryProtection.Execute, 16, _fd, IntPtr.Zero);
			if (ptr != Z.US(_start))
				throw new InvalidOperationException($"{nameof(mmap)}() returned NULL or the wrong pointer");
		}

		public void Deactivate()
		{
			var exitCode = munmap(Z.US(_start), Z.UU(_size));
			if (exitCode != 0)
				throw new InvalidOperationException($"{nameof(munmap)}() returned {exitCode}");
		}

		public void Protect(ulong start, ulong size, Protection prot)
		{
			var exitCode = mprotect(
				Z.US(start),
				Z.UU(size),
				prot.ToMemoryProtection()
			);
			if (exitCode != 0)
				throw new InvalidOperationException($"{nameof(mprotect)}() returned {exitCode}!");
		}
	}
}
