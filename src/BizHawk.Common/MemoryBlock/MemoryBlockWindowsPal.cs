using Windows.Win32.System.Memory;

using static BizHawk.Common.MemoryBlock;
using static Windows.Win32.Win32Imports;

namespace BizHawk.Common
{
	internal sealed class MemoryBlockWindowsPal : IMemoryBlockPal
	{
		public ulong Start { get; }
		private bool _disposed;

		public MemoryBlockWindowsPal(ulong size)
		{
			var ptr = (ulong)VirtualAlloc(
				UIntPtr.Zero,
				Z.UU(size),
				VIRTUAL_ALLOCATION_TYPE.MEM_RESERVE | VIRTUAL_ALLOCATION_TYPE.MEM_COMMIT,
				PAGE_PROTECTION_FLAGS.PAGE_NOACCESS);

			if (ptr == 0)
			{
				throw new InvalidOperationException($"{nameof(VirtualAlloc)}() returned NULL");
			}

			Start = ptr;
		}

		public void Protect(ulong start, ulong size, Protection prot)
		{
			if (!VirtualProtect(Z.UU(start), Z.UU(size), GetKernelMemoryProtectionValue(prot), out _))
			{
				throw new InvalidOperationException($"{nameof(VirtualProtect)}() returned FALSE!");
			}
		}

		private static PAGE_PROTECTION_FLAGS GetKernelMemoryProtectionValue(Protection prot) => prot switch
		{
			Protection.None => PAGE_PROTECTION_FLAGS.PAGE_NOACCESS,
			Protection.R => PAGE_PROTECTION_FLAGS.PAGE_READONLY,
			Protection.RW => PAGE_PROTECTION_FLAGS.PAGE_READWRITE,
			Protection.RX => PAGE_PROTECTION_FLAGS.PAGE_EXECUTE_READ,
			_ => throw new InvalidOperationException(nameof(prot)),
		};

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			VirtualFree(Z.UU(Start), UIntPtr.Zero, VIRTUAL_FREE_TYPE.MEM_RELEASE);
			_disposed = true;
		}
	}
}
