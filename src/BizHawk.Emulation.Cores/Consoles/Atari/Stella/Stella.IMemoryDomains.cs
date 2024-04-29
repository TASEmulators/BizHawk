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

		private void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>
			{
			};

			_memoryDomainsInit = true;
		}

		private void SyncAllByteArrayDomains()
		{
		}

		private void SyncByteArrayDomain(string name, byte[] data)
		{
		}
	}
}
