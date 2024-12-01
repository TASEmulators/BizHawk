using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Lynx
{
	public partial class Lynx
	{
		private void SetupMemoryDomains()
		{
			var mms = new List<MemoryDomain>
			{
				new MemoryDomainIntPtr("RAM", MemoryDomain.Endian.Little, LibLynx.GetRamPointer(Core), 65536, true, 2)
			};

			if (LibLynx.GetSaveRamPtr(Core, out var s, out var p))
			{
				mms.Add(new MemoryDomainIntPtr("Save RAM", MemoryDomain.Endian.Little, p, s, true, 2));
			}

			LibLynx.GetReadOnlyCartPtrs(Core, out var s0, out var p0, out var s1, out var p1);
			if (s0 > 0 && p0 != IntPtr.Zero)
			{
				mms.Add(new MemoryDomainIntPtr("Cart A", MemoryDomain.Endian.Little, p0, s0, false, 2));
			}

			if (s1 > 0 && p1 != IntPtr.Zero)
			{
				mms.Add(new MemoryDomainIntPtr("Cart B", MemoryDomain.Endian.Little, p1, s1, false, 2));
			}

			_memoryDomains = new MemoryDomainList(mms);
			((BasicServiceProvider) ServiceProvider).Register(_memoryDomains);
		}

		private IMemoryDomains _memoryDomains;
	}
}
