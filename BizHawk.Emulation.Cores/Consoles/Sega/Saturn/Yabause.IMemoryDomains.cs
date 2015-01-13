using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.Saturn
{
	public partial class Yabause : IMemoryDomains
	{
		public MemoryDomainList MemoryDomains { get; private set; }

		private void InitMemoryDomains()
		{
			var ret = new List<MemoryDomain>();
			var nmds = LibYabause.libyabause_getmemoryareas_ex();
			foreach (var nmd in nmds)
			{
				int l = nmd.length;
				IntPtr d = nmd.data;
				ret.Add(MemoryDomain.FromIntPtr(nmd.name, nmd.length, MemoryDomain.Endian.Little, nmd.data));
			}

			// main memory is in position 2
			MemoryDomains = new MemoryDomainList(ret, 2);
		}
	}
}
