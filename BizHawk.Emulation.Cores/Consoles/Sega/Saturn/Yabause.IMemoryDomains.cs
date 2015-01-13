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
				ret.Add(new MemoryDomain(
					nmd.name,
					nmd.length,
					MemoryDomain.Endian.Little,
					delegate(int addr)
					{
						if (addr < 0 || addr >= l)
							throw new ArgumentOutOfRangeException();
						unsafe
						{
							byte* p = (byte*)d;
							return p[addr];
						}
					},
					delegate(int addr, byte val)
					{
						if (addr < 0 || addr >= l)
							throw new ArgumentOutOfRangeException();
						unsafe
						{
							byte* p = (byte*)d;
							p[addr] = val;
						}
					}
				));
			}

			// fulfill the prophecy of MainMemory always being MemoryDomains[0]
			var tmp = ret[2];
			ret[2] = ret[0];
			ret[0] = tmp;
			MemoryDomains = new MemoryDomainList(ret);
		}
	}
}
