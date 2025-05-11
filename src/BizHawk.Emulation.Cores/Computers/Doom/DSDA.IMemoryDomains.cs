using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Doom
{
	public partial class DSDA
	{
		private IMemoryDomains MemoryDomains;

		private void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>
			{
				new MemoryDomainDelegate(
					"Things",
					0x1000000,
					MemoryDomain.Endian.Little,
					addr =>
					{
						if (addr > 0xFFFFFF) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
						return _core.dsda_read_memory_array(LibDSDA.MemoryArrayType.Things, (uint)addr);
					},
					null,
					1),
			};

			domains.Add(_elf.GetPagesDomain());
			MemoryDomains = new MemoryDomainList(domains) { };
			((BasicServiceProvider)ServiceProvider).Register(MemoryDomains);
		}
	}
}
