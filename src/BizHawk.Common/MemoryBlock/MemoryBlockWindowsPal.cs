using static BizHawk.Common.MemoryApiImports;
using static BizHawk.Common.MemoryBlock;

namespace BizHawk.Common
{
	internal sealed class MemoryBlockWindowsPal : IMemoryBlockPal
	{
		public ulong Start { get; }
		private bool _disposed;

		public MemoryBlockWindowsPal(ulong size)
		{
			var ptr = (ulong)VirtualAlloc(
				UIntPtr.Zero, Z.UU(size), AllocationType.MEM_RESERVE | AllocationType.MEM_COMMIT, MemoryProtection.NOACCESS);

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

		private static MemoryProtection GetKernelMemoryProtectionValue(Protection prot) => prot switch
		{
			Protection.None => MemoryProtection.NOACCESS,
			Protection.R => MemoryProtection.READONLY,
			Protection.RW => MemoryProtection.READWRITE,
			Protection.RX => MemoryProtection.EXECUTE_READ,
			_ => throw new InvalidOperationException(nameof(prot))
		};

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			VirtualFree(Z.UU(Start), UIntPtr.Zero, FreeType.Release);
			_disposed = true;
		}
	}
}
