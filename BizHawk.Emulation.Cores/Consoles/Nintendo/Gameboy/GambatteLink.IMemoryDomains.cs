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
				mm.Add(new WrappedMemoryDomain("L " + md.Name, md));
			}

			foreach (var md in R.MemoryDomains)
			{
				mm.Add(new WrappedMemoryDomain("R " + md.Name, md));
			}

			_memoryDomains = new MemoryDomainList(mm);
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(_memoryDomains);
		}
		
		// todo: clean this up
		private class WrappedMemoryDomain : MemoryDomain
		{
			private readonly MemoryDomain _m;

			public WrappedMemoryDomain(string name, MemoryDomain m)
			{
				_m = m;

				Name = name;
				Size = m.Size;
				WordSize = m.WordSize;
				EndianType = m.EndianType;
				Writable = m.Writable;
			}

			public override byte PeekByte(long addr)
			{
				return _m.PeekByte(addr);
			}

			public override void PokeByte(long addr, byte val)
			{
				_m.PokeByte(addr, val);
			}
		}
	}
}
