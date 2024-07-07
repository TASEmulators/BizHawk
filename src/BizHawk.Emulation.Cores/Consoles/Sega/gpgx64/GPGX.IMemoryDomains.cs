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
					{
						continue;
					}

					var name = Marshal.PtrToStringAnsi(pName)!;

					// typically Genesis domains will be 2 bytes large (and thus big endian and byteswapped)
					var oneByteWidth = name is "Z80 RAM" or "Main RAM" or "ROM" or "BOOT ROM" or "Cart (Volatile) RAM" or "SRAM";

					var endian = oneByteWidth
						? MemoryDomain.Endian.Little
						: MemoryDomain.Endian.Big;

					switch (name)
					{
						case "VRAM":
						{
							// vram pokes need to go through hook which invalidates cached tiles
							var p = (byte*)area;
							if (SystemId == VSystemID.Raw.GEN)
							{
								// Genesis has more VRAM, and GPGX byteswaps it
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
									wordSize: 1));
							}
							else
							{
								mm.Add(new MemoryDomainDelegate(name, size, MemoryDomain.Endian.Big,
									addr =>
									{
										if (addr is < 0 or > 0x3FFF) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
										using (_elf.EnterExit())
											return p![addr];
									},
									(addr, val) =>
									{
										if (addr is < 0 or > 0x3FFF) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
										Core.gpgx_poke_vram((int)addr, val);
									},
									wordSize: 1));
							}

							break;
						}
						case "CRAM":
						{
							var p = (byte*)area;
							if (SystemId == VSystemID.Raw.GEN)
							{
								// CRAM for Genesis in the core is internally a different format than what it is natively
								// this internal format isn't really useful, so let's convert it back
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
										Core.gpgx_poke_cram((int)addr, val);
									},
									wordSize: 2));
							}
							else
							{
								mm.Add(new MemoryDomainDelegate(name, size, MemoryDomain.Endian.Big,
									addr =>
									{
										if (addr is < 0 or > 0x3F) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
										using (_elf.EnterExit())
										{
											var c = *(ushort*)&p![addr & ~1];
											return (byte)((addr & 1) != 0 ? c & 0xFF : c >> 8);
										}
									},
									(addr, val) =>
									{
										if (addr is < 0 or > 0x3F) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
										Core.gpgx_poke_cram((int)addr, val);
									},
									wordSize: 2));
							}

							break;
						}
						default:
						{
							if (oneByteWidth)
							{
								mm.Add(new MemoryDomainIntPtrMonitor(name, endian, area, size, true, 1, _elf));
							}
							else
							{
								mm.Add(new MemoryDomainIntPtrSwap16Monitor(name, endian, area, size, true, _elf));
							}

							break;
						}
					}
				}

				MemoryDomain systemBus;
				if (SystemId == VSystemID.Raw.GEN)
				{
					systemBus = new MemoryDomainDelegate("M68K BUS", 0x1000000, MemoryDomain.Endian.Big,
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

					mm.Add(systemBus);

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
				}
				else
				{
					systemBus = new MemoryDomainDelegate("Z80 BUS", 0x10000, MemoryDomain.Endian.Little,
						addr =>
						{
							var a = (uint)addr;
							if (a > 0xFFFF) throw new ArgumentOutOfRangeException(paramName: nameof(addr), a, message: "address out of range");
							return Core.gpgx_peek_z80_bus(a);
						},
						(addr, val) =>
						{
							var a = (uint)addr;
							if (a > 0xFFFF) throw new ArgumentOutOfRangeException(paramName: nameof(addr), a, message: "address out of range");
							Core.gpgx_write_z80_bus(a, val);
						}, 1);

					mm.Add(systemBus);
				}

				mm.Add(_elf.GetPagesDomain());
				_memoryDomains = new MemoryDomainList(mm) { SystemBus = systemBus };
				((BasicServiceProvider) ServiceProvider).Register(_memoryDomains);
			}
		}
	}
}
