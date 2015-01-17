using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class GambatteLink 
	{
		private IMemoryDomains _memoryDomains;

		private void SetMemoryDomains()
		{
			var mm = new List<MemoryDomain>();

			foreach (var md in L.MemoryDomains)
			{
				mm.Add(new MemoryDomain("L " + md.Name, md.Size, md.EndianType, md.PeekByte, md.PokeByte));
			}

			foreach (var md in R.MemoryDomains)
			{
				mm.Add(new MemoryDomain("R " + md.Name, md.Size, md.EndianType, md.PeekByte, md.PokeByte));
			}

			_memoryDomains = new MemoryDomainList(mm);
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(_memoryDomains);
		}
	}
}
