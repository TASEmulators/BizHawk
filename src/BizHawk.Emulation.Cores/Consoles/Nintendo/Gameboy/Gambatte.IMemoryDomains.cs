using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class Gameboy
	{
		private readonly List<MemoryDomain> _memoryDomains = new List<MemoryDomain>();
		internal IMemoryDomains MemoryDomains { get; private set; }

		private void CreateMemoryDomain(LibGambatte.MemoryAreas which, string name)
		{
			IntPtr data = IntPtr.Zero;
			int length = 0;

			if (!LibGambatte.gambatte_getmemoryarea(GambatteState, which, ref data, ref length))
			{
				throw new Exception($"{nameof(LibGambatte.gambatte_getmemoryarea)}() failed!");
			}

			// if length == 0, it's an empty block; (usually rambank on some carts); that's ok
			if (data != IntPtr.Zero && length > 0)
			{
				_memoryDomains.Add(new MemoryDomainIntPtr(name, MemoryDomain.Endian.Little, data, length, true, 1));
			}
		}

		private void InitMemoryDomains()
		{
			CreateMemoryDomain(LibGambatte.MemoryAreas.wram, "WRAM");
			CreateMemoryDomain(LibGambatte.MemoryAreas.rom, "ROM");
			CreateMemoryDomain(LibGambatte.MemoryAreas.vram, "VRAM");
			CreateMemoryDomain(LibGambatte.MemoryAreas.oam, "OAM");
			CreateMemoryDomain(LibGambatte.MemoryAreas.hram, "HRAM");

			// also add a special memory domain for the system bus, where calls get sent directly to the core each time
			_memoryDomains.Add(new MemoryDomainDelegate("System Bus", 65536, MemoryDomain.Endian.Little,
				delegate(long addr)
				{
					if (addr < 0 || addr >= 65536)
					{
						throw new ArgumentOutOfRangeException();
					}

					return LibGambatte.gambatte_cpuread(GambatteState, (ushort)addr);
				},
				delegate(long addr, byte val)
				{
					if (addr < 0 || addr >= 65536)
					{
						throw new ArgumentOutOfRangeException();
					}

					LibGambatte.gambatte_cpuwrite(GambatteState, (ushort)addr, val);
				}, 1));

			CreateMemoryDomain(LibGambatte.MemoryAreas.cartram, "CartRAM");

			MemoryDomains = new MemoryDomainList(_memoryDomains);
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(MemoryDomains);
		}
	}
}
