using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Sega.gpgx
{
	public partial class GPGX
	{
		private IMemoryDomains MemoryDomains;

		private unsafe void SetMemoryDomains()
		{
			var mm = new List<MemoryDomain>();
			for (int i = LibGPGX.MIN_MEM_DOMAIN; i <= LibGPGX.MAX_MEM_DOMAIN; i++)
			{
				IntPtr area = IntPtr.Zero;
				int size = 0;
				IntPtr pname = Core.gpgx_get_memdom(i, ref area, ref size);
				if (area == IntPtr.Zero || pname == IntPtr.Zero || size == 0)
					continue;
				string name = Marshal.PtrToStringAnsi(pname);
				if (name == "VRAM")
				{
					// vram pokes need to go through hook which invalidates cached tiles
					byte* p = (byte*)area;
					mm.Add(new MemoryDomain(name, size, MemoryDomain.Endian.Unknown,
						delegate(long addr)
						{
							if (addr < 0 || addr >= 65536)
								throw new ArgumentOutOfRangeException();
							return p[addr ^ 1];
						},
						delegate(long addr, byte val)
						{
							if (addr < 0 || addr >= 65536)
								throw new ArgumentOutOfRangeException();
							Core.gpgx_poke_vram(((int)addr) ^ 1, val);
						},
						byteSize: 2));
				}

				else
				{
					var byteSize = name.Contains("Z80") ? 1 : 2;
					mm.Add(MemoryDomain.FromIntPtrSwap16(name, size,
						MemoryDomain.Endian.Big, area, name != "MD CART" && name != "CD BOOT ROM", byteSize));
				}
			}
			var m68Bus = new MemoryDomain("M68K BUS", 0x1000000, MemoryDomain.Endian.Big,
				delegate (long addr)
				{
					var a = (uint)addr;
					if (a >= 0x1000000)
						throw new ArgumentOutOfRangeException();
					return Core.gpgx_peek_m68k_bus(a);
				},
				delegate (long addr, byte val)
				{
					var a = (uint)addr;
					if (a >= 0x1000000)
						throw new ArgumentOutOfRangeException();
					Core.gpgx_write_m68k_bus(a, val);
				}, 2);

			mm.Add(m68Bus);

			var s68Bus = new MemoryDomain("S68K BUS", 0x1000000, MemoryDomain.Endian.Big,
				delegate (long addr)
				{
					var a = (uint)addr;
					if (a >= 0x1000000)
						throw new ArgumentOutOfRangeException();
					return Core.gpgx_peek_s68k_bus(a);
				},
				delegate (long addr, byte val)
				{
					var a = (uint)addr;
					if (a >= 0x1000000)
						throw new ArgumentOutOfRangeException();
					Core.gpgx_write_s68k_bus(a, val);
				}, 2);

			if (IsSegaCD)
			{
				mm.Add(s68Bus);
			}

			MemoryDomains = new MemoryDomainList(mm);
			MemoryDomains.SystemBus = m68Bus;

			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(MemoryDomains);
		}
	}
}
