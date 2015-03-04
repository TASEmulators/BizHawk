using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.Saturn
{
	public partial class Yabause
	{
		private IMemoryDomains _memoryDomains;

		private void InitMemoryDomains()
		{
			var ret = new List<MemoryDomain>();
			var nmds = LibYabause.libyabause_getmemoryareas_ex();
			foreach (var nmd in nmds)
			{
				int l = nmd.length;
				IntPtr d = nmd.data;
				ret.Add(MemoryDomain.FromIntPtr(nmd.name, nmd.length, MemoryDomain.Endian.Little, nmd.data, true, 4));
			}

			// main memory is in position 2
			_memoryDomains = new MemoryDomainList(ret);
			_memoryDomains.MainMemory = _memoryDomains["Work Ram Low"];

			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(_memoryDomains);
		}
	}
}
