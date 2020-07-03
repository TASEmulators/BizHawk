using System;
using System.Runtime.InteropServices;
using static BizHawk.BizInvoke.MemoryBlock;
using static BizHawk.BizInvoke.POSIXLibC;

namespace BizHawk.BizInvoke
{
	internal sealed unsafe class MemoryBlockLinuxPal : IMemoryBlockPal
	{
		/*
		Differences compared with MemoryBlockWindowsPal:
			1) Commit is handled by only mapping up to the commit size, and then expanding commit is handled by unmap + truncate + remap.
				So all unmanaged structures (including LinGuard) are always looking at the committed size, not total size.
			2) Because of sigaltstack, RW_Stack is not needed and is made to behave the same as regular write guarding.
		*/

		/// <summary>handle returned by <see cref="memfd_create"/></summary>
		private int _fd = -1;
		private ulong _start;
		private ulong _size;
		private ulong _committedSize;
		private bool _active;

		/// <summary>
		/// Reserve bytes to later be swapped in, but do not map them
		/// </summary>
		/// <param name="start">eventual mapped address</param>
		/// <param name="size"></param>
		/// <exception cref="InvalidOperationException">
		/// failed to get file descriptor
		/// </exception>
		public MemoryBlockLinuxPal(ulong start, ulong size)
		{
			_start = start;
			_size = size;
			_fd = memfd_create("MemoryBlockUnix", 1 /*MFD_CLOEXEC*/);
			if (_fd == -1)
				throw new InvalidOperationException($"{nameof(memfd_create)}() failed with error {Marshal.GetLastWin32Error()}");
		}

		public void Dispose()
		{
			if (_fd == -1)
				return;
			if (_active)
			{
				try
				{
					Deactivate();
				}
				catch
				{}
			}
			close(_fd);
			_fd = -1;
			GC.SuppressFinalize(this);
		}

		~MemoryBlockLinuxPal()
		{
			Dispose();
		}

		public void Activate()
		{
			if (_committedSize > 0)
			{
				var ptr = mmap(Z.US(_start), Z.UU(_committedSize),
					MemoryProtection.Read | MemoryProtection.Write | MemoryProtection.Execute,
					17, // MAP_SHARED | MAP_FIXED
					_fd, IntPtr.Zero);
				if (ptr != Z.US(_start))
				{
					throw new InvalidOperationException($"{nameof(mmap)}() failed with error {Marshal.GetLastWin32Error()}");
				}
			}
			_active = true;
		}

		public void Deactivate()
		{
			if (_committedSize > 0)
			{
				var errorCode = munmap(Z.US(_start), Z.UU(_committedSize));
				if (errorCode != 0)
					throw new InvalidOperationException($"{nameof(munmap)}() failed with error {Marshal.GetLastWin32Error()}");
			}
			_active = false;
		}

		public void Commit(ulong newCommittedSize)
		{
			var errorCode = ftruncate(_fd, Z.US(newCommittedSize));
			if (errorCode != 0)
				throw new InvalidOperationException($"{nameof(ftruncate)}() failed with error {Marshal.GetLastWin32Error()}");
			// map in the previously unmapped portions contiguously
			var ptr = mmap(Z.US(_start + _committedSize), Z.UU(newCommittedSize - _committedSize),
				MemoryProtection.Read | MemoryProtection.Write | MemoryProtection.Execute,
				17, // MAP_SHARED | MAP_FIXED
				_fd, Z.US(_committedSize));
			if (ptr != Z.US(_start + _committedSize))
			{
				throw new InvalidOperationException($"{nameof(mmap)}() failed with error {Marshal.GetLastWin32Error()}");
			}
			_committedSize = newCommittedSize;
		}

		private static MemoryProtection ToMemoryProtection(Protection prot)
		{
			switch (prot)
			{
				case Protection.None:
					return MemoryProtection.None;
				case Protection.R:
					return MemoryProtection.Read;
				case Protection.RW:
					return MemoryProtection.Read | MemoryProtection.Write;
				case Protection.RX:
					return MemoryProtection.Read | MemoryProtection.Execute;
				default:
					throw new ArgumentOutOfRangeException(nameof(prot));
			}
		}

		public void Protect(ulong start, ulong size, Protection prot)
		{
			var errorCode = mprotect(
				Z.US(start),
				Z.UU(size),
				ToMemoryProtection(prot)
			);
			if (errorCode != 0)
				throw new InvalidOperationException($"{nameof(mprotect)}() failed with error {Marshal.GetLastWin32Error()}!");
		}
	}
}
