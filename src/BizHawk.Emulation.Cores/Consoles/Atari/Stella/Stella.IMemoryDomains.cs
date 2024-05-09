using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Stella
{
	public partial class Stella
	{
		internal IMemoryDomains MemoryDomains;
		private readonly Dictionary<string, MemoryDomainByteArray> _byteArrayDomains = new Dictionary<string, MemoryDomainByteArray>();
		private bool _memoryDomainsInit;
		private uint _cartMemSize;

		private void SetupMemoryDomains()
		{
			_cartMemSize = Core.stella_get_cartram_size();
			var mainRamAddress = IntPtr.Zero;
			var cartDPCRamAddress = IntPtr.Zero;
			Core.stella_get_mainram_ptr(ref mainRamAddress);

			var domains = new List<MemoryDomain>
			{
				new MemoryDomainDelegate(
					"TIA",
					16,
					MemoryDomain.Endian.Little,
					addr => Core.stella_peek_tia((uint)addr),
					(addr, value) => Core.stella_poke_tia((uint)addr, value),
					1),

				new MemoryDomainDelegate(
					"PIA",
					1024,
					MemoryDomain.Endian.Little,
					addr => Core.stella_peek_m6532((uint)addr),
					(addr, value) => Core.stella_poke_m6532((uint)addr, value),
					1),

				new MemoryDomainDelegate(
					"System Bus",
					65536,
					MemoryDomain.Endian.Little,
					addr => Core.stella_peek_systembus((uint) addr),
					(addr, value) => Core.stella_poke_systembus((uint) addr, value),
					1)
			};

			if (_cartMemSize > 0)
			{
				domains.Add(new MemoryDomainDelegate(
					"Cart Ram",
					_cartMemSize,
					MemoryDomain.Endian.Little,
					addr => Core.stella_peek_cartram((uint)addr),
					(addr, value) => Core.stella_poke_cartram((uint)addr, value),
					1));
			}

			domains.Add(new MemoryDomainIntPtrMonitor("Main RAM", MemoryDomain.Endian.Little, mainRamAddress, 128, true, 1, _elf));

			MemoryDomains = new MemoryDomainList(_byteArrayDomains.Values.Concat(domains).ToList());
			((BasicServiceProvider)ServiceProvider).Register(MemoryDomains);

			_memoryDomainsInit = true;
		}
	}
}
