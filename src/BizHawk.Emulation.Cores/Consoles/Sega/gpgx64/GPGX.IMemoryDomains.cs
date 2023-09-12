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
				List<MemoryDomain> mm = new();
				for (int i = LibGPGX.MIN_MEM_DOMAIN; i <= LibGPGX.MAX_MEM_DOMAIN; i++)
				{
					var area = IntPtr.Zero;
					int size = 0;
					var pName = Core.gpgx_get_memdom(i, ref area, ref size);
					if (area == IntPtr.Zero || pName == IntPtr.Zero || size == 0)
						continue;
					string name = Marshal.PtrToStringAnsi(pName)!;

					var endian = name == "Z80 RAM"
							? MemoryDomain.Endian.Little
							: MemoryDomain.Endian.Big;

					if (name == "VRAM")
					{
						// vram pokes need to go through hook which invalidates cached tiles
						byte* p = (byte*)area;
						mm.Add(new MemoryDomainDelegate(name, size, MemoryDomain.Endian.Big,
							addr =>
							{
								if (addr is < 0 or > 0xFFFF) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
								using (_elf.EnterExit())
									return p![addr ^ 1];
							},
							(addr, val) =>
							{
								if (addr is < 0 or > 0xFFFF) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
								Core.gpgx_poke_vram((int)addr ^ 1, val);
							},
							wordSize: 2));
					}
					else if (name == "CRAM")
					{
						// CRAM in the core is internally a different format than what it is natively
						// this internal format isn't really useful, so let's convert it back
						byte* p = (byte*)area;
						mm.Add(new MemoryDomainDelegate(name, size, MemoryDomain.Endian.Big,
							addr =>
							{
								if (addr is < 0 or > 0x7F) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
								using (_elf.EnterExit())
								{
									ushort c = *(ushort*)&p![addr & ~1];
									c = (ushort)(((c & 0x1C0) << 3) | ((c & 0x038) << 2) | ((c & 0x007) << 1));
									return (byte)((addr & 1) != 0 ? c & 0xFF : c >> 8);
								}
							},
							(addr, val) =>
							{
								if (addr is < 0 or > 0x7F) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
								Core.gpgx_poke_cram((int)addr, val);
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
				MemoryDomainDelegate m68Bus = new("M68K BUS", 0x1000000, MemoryDomain.Endian.Big,
					addr =>
					{
						uint a = (uint)addr;
						if (a > 0xFFFFFF) throw new ArgumentOutOfRangeException(paramName: nameof(addr), a, message: "address out of range");
						return Core.gpgx_peek_m68k_bus(a);
					},
					(addr, val) =>
					{
						uint a = (uint)addr;
						if (a > 0xFFFFFF) throw new ArgumentOutOfRangeException(paramName: nameof(addr), a, message: "address out of range");
						Core.gpgx_write_m68k_bus(a, val);
					}, 2);

				mm.Add(m68Bus);

				if (IsMegaCD)
				{
					MemoryDomainDelegate s68Bus = new("S68K BUS", 0x1000000, MemoryDomain.Endian.Big,
					addr =>
					{
						uint a = (uint)addr;
						if (a > 0xFFFFFF) throw new ArgumentOutOfRangeException(paramName: nameof(addr), a, message: "address out of range");
						return Core.gpgx_peek_s68k_bus(a);
					},
					(addr, val) =>
					{
						uint a = (uint)addr;
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
