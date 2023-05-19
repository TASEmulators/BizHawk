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
				for (var i = LibGPGX.MIN_MEM_DOMAIN; i <= LibGPGX.MAX_MEM_DOMAIN; i++)
				{
					var area = IntPtr.Zero;
					var size = 0;
					var pName = Core.gpgx_get_memdom(i, ref area, ref size);
					if (area == IntPtr.Zero || pName == IntPtr.Zero || size == 0)
						continue;
					var name = Marshal.PtrToStringAnsi(pName)!;

					var endian = name == "Z80 RAM"
							? MemoryDomain.Endian.Little
							: MemoryDomain.Endian.Big;

					if (name == "VRAM")
					{
						// vram pokes need to go through hook which invalidates cached tiles
						var p = (byte*)area;
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
						var p = (byte*)area;
						mm.Add(new MemoryDomainDelegate(name, size, MemoryDomain.Endian.Big,
							addr =>
							{
								if (addr is < 0 or > 0x7F) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
								using (_elf.EnterExit())
								{
									var c = *(ushort*)&p![addr & ~1];
									c = (ushort)(((c & 0x1C0) << 3) | ((c & 0x038) << 2) | ((c & 0x007) << 1));
									return (byte)((addr & 1) != 0 ? c & 0xFF : c >> 8);
								}
							},
							(addr, val) =>
							{
								if (addr is < 0 or > 0x7F) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
								using (_elf.EnterExit())
								{
									var c = *(ushort*)&p![addr & ~1];
									c = (ushort)(((c & 0x1C0) << 3) | ((c & 0x038) << 2) | ((c & 0x007) << 1));
									if ((addr & 1) != 0)
									{
										c &= 0xFF00;
										c |= val;
									}
									else
									{
										c &= 0x00FF;
										c |= (ushort)(val << 8);
									}
									c = (ushort)(((c & 0xE00) >> 3) | ((c & 0x0E0) >> 2) | ((c & 0x00E) >> 1)); 
									*(ushort*)&p![addr & ~1] = c;
								}
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
