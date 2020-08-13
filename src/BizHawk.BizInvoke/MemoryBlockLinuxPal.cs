using System;
using System.Runtime.InteropServices;
using static BizHawk.BizInvoke.MemoryBlock;
using static BizHawk.BizInvoke.POSIXLibC;

namespace BizHawk.BizInvoke
{
	internal sealed unsafe class MemoryBlockLinuxPal : IMemoryBlockPal
	{
		public ulong Start { get; }
		private readonly ulong _size;
		private bool _disposed;

		/// <summary>
		/// Map some bytes
		/// </summary>
		/// <param name="size"></param>
		/// <exception cref="InvalidOperationException">
		/// failed to mmap
		/// </exception>
		public MemoryBlockLinuxPal(ulong size)
		{
			var ptr = (ulong)mmap(IntPtr.Zero, Z.UU(size), MemoryProtection.None, 0x22 /* MAP_PRIVATE | MAP_ANON */, -1, IntPtr.Zero);
			if (ptr == ulong.MaxValue)
				throw new InvalidOperationException($"{nameof(mmap)}() failed with error {Marshal.GetLastWin32Error()}");
			_size = size;
			Start = ptr;
		}

		public void Dispose()
		{
			if (_disposed)
				return;
			munmap(Z.US(Start), Z.UU(_size));
			_disposed = true;
			GC.SuppressFinalize(this);
		}

		~MemoryBlockLinuxPal()
		{
			Dispose();
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
