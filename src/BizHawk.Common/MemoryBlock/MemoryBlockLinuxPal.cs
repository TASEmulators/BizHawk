using System.Runtime.InteropServices;

using static BizHawk.Common.MemoryBlock;
using static BizHawk.Common.MmanImports;

namespace BizHawk.Common
{
	internal sealed class MemoryBlockLinuxPal : IMemoryBlockPal
	{
		public ulong Start { get; }
		private readonly ulong _size;
		private bool _disposed;

		/// <summary>
		/// Map some bytes
		/// </summary>
		/// <exception cref="InvalidOperationException">
		/// failed to mmap
		/// </exception>
		public MemoryBlockLinuxPal(ulong size)
		{
			var ptr = mmap(IntPtr.Zero, Z.UU(size), MemoryProtection.None, 0x22 /* MAP_PRIVATE | MAP_ANON */, -1, IntPtr.Zero);
			if (ptr == new IntPtr(-1))
			{
				throw new InvalidOperationException($"{nameof(mmap)}() failed with error {Marshal.GetLastWin32Error()}");
			}

			_size = size;
			Start = (ulong)ptr;
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_ = munmap(Z.US(Start), Z.UU(_size));
			_disposed = true;
		}

		private static MemoryProtection ToMemoryProtection(Protection prot) => prot switch
		{
			Protection.None => MemoryProtection.None,
			Protection.R => MemoryProtection.Read,
			Protection.RW => MemoryProtection.Read | MemoryProtection.Write,
			Protection.RX => MemoryProtection.Read | MemoryProtection.Execute,
			_ => throw new InvalidOperationException(nameof(prot))
		};

		public void Protect(ulong start, ulong size, Protection prot)
		{
			var errorCode = mprotect(
				Z.US(start),
				Z.UU(size),
				ToMemoryProtection(prot)
			);

			if (errorCode != 0)
			{
				throw new InvalidOperationException($"{nameof(mprotect)}() failed with error {Marshal.GetLastWin32Error()}!");
			}
		}
	}
}
