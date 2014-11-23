using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Calculators
{
	public partial class TI83 : IMemoryDomains
	{
		public MemoryDomainList MemoryDomains
		{
			get { return _memoryDomains; }
		}

		private void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>
			{
				new MemoryDomain(
					"Main RAM",
					ram.Length,
					MemoryDomain.Endian.Little,
					addr => ram[addr],
					(addr, value) => ram[addr] = value
				)
			};

			_memoryDomains = new MemoryDomainList(domains);
		}

		private MemoryDomainList _memoryDomains;
	}
}
