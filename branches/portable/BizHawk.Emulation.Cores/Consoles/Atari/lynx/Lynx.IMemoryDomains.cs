using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Lynx
{
	public partial class Lynx
	{
		private void SetupMemoryDomains()
		{
			var mms = new List<MemoryDomain>();
			mms.Add(MemoryDomain.FromIntPtr("RAM", 65536, MemoryDomain.Endian.Little, LibLynx.GetRamPointer(Core), true, 2));

			IntPtr p;
			int s;
			if (LibLynx.GetSaveRamPtr(Core, out s, out p))
			{
				mms.Add(MemoryDomain.FromIntPtr("Save RAM", s, MemoryDomain.Endian.Little, p, true, 2));
			}

			IntPtr p0, p1;
			int s0, s1;
			LibLynx.GetReadOnlyCartPtrs(Core, out s0, out p0, out s1, out p1);
			if (s0 > 0 && p0 != IntPtr.Zero)
				mms.Add(MemoryDomain.FromIntPtr("Cart A", s0, MemoryDomain.Endian.Little, p0, false, 2));
			if (s1 > 0 && p1 != IntPtr.Zero)
				mms.Add(MemoryDomain.FromIntPtr("Cart B", s1, MemoryDomain.Endian.Little, p1, false, 2));

			_memoryDomains = new MemoryDomainList(mms);
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(_memoryDomains);
		}

		private IMemoryDomains _memoryDomains;
	}
}
