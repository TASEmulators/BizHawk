using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Consoles.Sega.gpgx
{
	public partial class GPGX
	{
		private IMemoryDomains _memoryDomains;

		private unsafe void SetMemoryDomains()
		{
			using (_elf.EnterExit())
			{
				var mm = new List<MemoryDomain>();
				for (int i = LibGPGX.MIN_MEM_DOMAIN; i <= LibGPGX.MAX_MEM_DOMAIN; i++)
				{
					IntPtr area = IntPtr.Zero;
					int size = 0;
					IntPtr pName = Core.gpgx_get_memdom(i, ref area, ref size);
					if (area == IntPtr.Zero || pName == IntPtr.Zero || size == 0)
						continue;
					string name = Marshal.PtrToStringAnsi(pName);

					var endian = name == "Z80 RAM"
							? MemoryDomain.Endian.Little
							: MemoryDomain.Endian.Big;

					if (name == "VRAM")
					{
						// vram pokes need to go through hook which invalidates cached tiles
						byte* p = (byte*)area;
						mm.Add(new MemoryDomainDelegate(name, size, MemoryDomain.Endian.Unknown,
							addr =>
							{
								if (addr is < 0 or > 0xFFFF) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
								using (_elf.EnterExit())
									return p[addr ^ 1];
							},
							(addr, val) =>
							{
								if (addr is < 0 or > 0xFFFF) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
								Core.gpgx_poke_vram(((int)addr) ^ 1, val);
							},
							wordSize: 2));
					}
					else if (name.Contains("Z80"))
					{
						mm.Add(new MemoryDomainIntPtrMonitor(name, endian, area, size, true, 1, _elf));
					}
					else
					{
						mm.Add(new MemoryDomainIntPtrSwap16Monitor(name, endian, area, size, true, _elf));
					}
				}
				var m68Bus = new MemoryDomainDelegate("M68K BUS", 0x1000000, MemoryDomain.Endian.Big,
					addr =>
					{
						var a = (uint)addr;
						if (a > 0xFFFFFF) throw new ArgumentOutOfRangeException(paramName: nameof(addr), a, message: "address out of range");
						return Core.gpgx_peek_m68k_bus(a);
					},
					(addr, val) =>
					{
						var a = (uint)addr;
						if (a > 0xFFFFFF) throw new ArgumentOutOfRangeException(paramName: nameof(addr), a, message: "address out of range");
						Core.gpgx_write_m68k_bus(a, val);
					}, 2);

				mm.Add(m68Bus);

				if (IsMegaCD)
				{
					var s68Bus = new MemoryDomainDelegate("S68K BUS", 0x1000000, MemoryDomain.Endian.Big,
					addr =>
					{
						var a = (uint)addr;
						if (a > 0xFFFFFF) throw new ArgumentOutOfRangeException(paramName: nameof(addr), a, message: "address out of range");
						return Core.gpgx_peek_s68k_bus(a);
					},
					(addr, val) =>
					{
						var a = (uint)addr;
						if (a > 0xFFFFFF) throw new ArgumentOutOfRangeException(paramName: nameof(addr), a, message: "address out of range");
						Core.gpgx_write_s68k_bus(a, val);
					}, 2);


					mm.Add(s68Bus);
				}
				mm.Add(_elf.GetPagesDomain());

				_memoryDomains = new MemoryDomainList(mm) { SystemBus = m68Bus };
				((BasicServiceProvider) ServiceProvider).Register(_memoryDomains);
			}
		}
	}
}
