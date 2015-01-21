using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Calculators
{
	public partial class TI83
	{
		private void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>
			{
				MemoryDomain.FromByteArray("Main RAM", MemoryDomain.Endian.Little, ram)
			};

			_memoryDomains = new MemoryDomainList(domains);
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(_memoryDomains);
		}

		private IMemoryDomains _memoryDomains;
	}
}
