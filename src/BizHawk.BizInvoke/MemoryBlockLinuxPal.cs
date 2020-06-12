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
			if ((IntPtr)LinGuard.AddTripGuard(Z.UU(_start), Z.UU(_size)) == IntPtr.Zero)
			{
				throw new InvalidOperationException($"{nameof(LinGuard.AddTripGuard)}() returned NULL");
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
			if (!LinGuard.RemoveTripGuard(Z.UU(_start), Z.UU(_size)))
				throw new InvalidOperationException($"{nameof(LinGuard.RemoveTripGuard)}() returned FALSE");
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
				case Protection.RW_Invisible:
					return MemoryProtection.Read | MemoryProtection.Write;
				case Protection.RW_Stack:
					// because of sigaltstack, LinGuard has no issues with readonly stacks and the special distinction that
					// the windows port draws between stack vs non stack memory is ignored here
					return MemoryProtection.Read;
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

		public void GetWriteStatus(WriteDetectionStatus[] dest, Protection[] pagedata)
		{
			var p = (IntPtr)LinGuard.ExamineTripGuard(Z.UU(_start), Z.UU(_size));
			if (p == IntPtr.Zero)
				throw new InvalidOperationException($"{nameof(LinGuard.ExamineTripGuard)}() returned NULL!");
			Marshal.Copy(p, (byte[])(object)dest, 0, dest.Length);
		}

		public void SetWriteStatus(WriteDetectionStatus[] src)
		{
			var p = (IntPtr)LinGuard.ExamineTripGuard(Z.UU(_start), Z.UU(_size));
			if (p == IntPtr.Zero)
				throw new InvalidOperationException($"{nameof(LinGuard.ExamineTripGuard)}() returned NULL!");
			Marshal.Copy((byte[])(object)src, 0, p, src.Length);
		}

		private static unsafe class LinGuard
		{
			/// <summary>
			/// Add write detection to an area of memory.  Any page in the specified range that has CanChange
			/// set and triggers an access violation on write
			/// will be noted, set to read+write permissions, and execution will be continued.
			/// CALLER'S RESPONSIBILITY: All addresses are page aligned.
			/// CALLER'S RESPONSIBILITY: No other thread enters any LinGuard function, or trips any tracked page during this call.
			/// CALLER'S RESPONSIBILITY: Pages to be tracked are mprotected to R.  Pages with write permission
			/// cause no issues, but they will not trip.
			/// </summary>
			/// <returns>The same information as ExamineTripGuard, or null on failure</returns>
			[DllImport("linguard.so")]
			public static extern WriteDetectionStatus* AddTripGuard(UIntPtr start, UIntPtr length);
			/// <summary>
			/// Remove write detection from the specified addresses.
			/// CALLER'S RESPONSIBILITY: All addresses are page aligned.
			/// CALLER'S RESPONSIBILITY: No other thread enters any LinGuard function, or trips any tracked guard page during this call.
			/// </summary>
			/// <returns>false on failure (usually, the address range did not match a known one)</returns>
			[DllImport("linguard.so")]
			public static extern bool RemoveTripGuard(UIntPtr start, UIntPtr length);
			/// <summary>
			/// Examines a previously installed guard page detection.
			/// CALLER'S RESPONSIBILITY: All addresses are page aligned.
			/// CALLER'S RESPONSIBILITY: No other thread enters any LinGuard function, or trips any tracked guard page during this call.
			/// </summary>
			/// <returns>
			/// A pointer to an array of bytes, one byte for each memory page in the range.  Caller should set CanChange on pages to
			/// observe, and read back DidChange to see if things changed.
			/// </returns>
			[DllImport("linguard.so")]
			public static extern WriteDetectionStatus* ExamineTripGuard(UIntPtr start, UIntPtr length);
		}
	}
}
