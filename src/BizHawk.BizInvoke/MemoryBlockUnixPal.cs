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
		private ulong _committedSize;

		/// <summary>
		/// Reserve bytes to later be swapped in, but do not map them
		/// </summary>
		/// <param name="start">eventual mapped address</param>
		/// <param name="size"></param>
		/// <exception cref="InvalidOperationException">
		/// failed to get file descriptor
		/// </exception>
		public MemoryBlockUnixPal(ulong start, ulong size)
		{
			_start = start;
			_size = size;
			_fd = memfd_create("MemoryBlockUnix", 0);
			if (_fd == -1)
				throw new InvalidOperationException($"{nameof(memfd_create)}() returned -1");
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
			if (_committedSize > 0)
			{
				var ptr = mmap(Z.US(_start), Z.UU(_committedSize),
					MemoryProtection.Read | MemoryProtection.Write | MemoryProtection.Execute,
					16, // MAP_FIXED
					_fd, IntPtr.Zero);
				if (ptr != Z.US(_start))
					throw new InvalidOperationException($"{nameof(mmap)}() returned NULL or the wrong pointer");
			}
		}

		public void Deactivate()
		{
			if (_committedSize > 0)
			{
				var errorCode = munmap(Z.US(_start), Z.UU(_committedSize));
				if (errorCode != 0)
					throw new InvalidOperationException($"{nameof(munmap)}() returned {errorCode}");
			}
		}

		public void Commit(ulong length)
		{
			Deactivate();
			var errorCode = ftruncate(_fd, Z.US(length));
			if (errorCode != 0)
				throw new InvalidOperationException($"{nameof(ftruncate)}() returned {errorCode}");
			Activate();
		}

		public void Protect(ulong start, ulong size, Protection prot)
		{
			var errorCode = mprotect(
				Z.US(start),
				Z.UU(size),
				prot.ToMemoryProtection()
			);
			if (errorCode != 0)
				throw new InvalidOperationException($"{nameof(mprotect)}() returned {errorCode}!");
		}
	}
}
