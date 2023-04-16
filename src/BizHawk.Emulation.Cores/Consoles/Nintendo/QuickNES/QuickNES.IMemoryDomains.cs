using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES
{
	public partial class QuickNES
	{
		private void InitMemoryDomains()
		{
			List<MemoryDomain> mm = new List<MemoryDomain>();
			for (int i = 0; ; i++)
			{
				IntPtr data = IntPtr.Zero;
				int size = 0;
				bool writable = false;
				IntPtr name = IntPtr.Zero;

				if (!QN.qn_get_memory_area(Context, i, ref data, ref size, ref writable, ref name))
					break;

				if (data != IntPtr.Zero && size > 0 && name != IntPtr.Zero)
				{
					mm.Add(new MemoryDomainIntPtr(Marshal.PtrToStringAnsi(name), MemoryDomain.Endian.Little, data, size, writable, 1));
				}
			}

			// add system bus
			mm.Add(new MemoryDomainDelegate(
				"System Bus",
				0x10000,
				MemoryDomain.Endian.Unknown,
				delegate(long addr)
				{
					if (addr < 0 || addr >= 0x10000)
					{
						throw new ArgumentOutOfRangeException();
					}

					return QN.qn_peek_prgbus(Context, (int)addr);
				},
				delegate(long addr, byte val)
				{
					if (addr < 0 || addr >= 0x10000)
					{
						throw new ArgumentOutOfRangeException();
					}

					QN.qn_poke_prgbus(Context, (int)addr, val);
				}, 1));

			_memoryDomains = new MemoryDomainList(mm);
			((BasicServiceProvider) ServiceProvider).Register(_memoryDomains);
		}

		private IMemoryDomains _memoryDomains;
	}
}
