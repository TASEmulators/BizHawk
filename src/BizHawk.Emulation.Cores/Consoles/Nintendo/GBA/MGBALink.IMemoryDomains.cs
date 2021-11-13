using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class MGBALink 
	{
		private IMemoryDomains _memoryDomains;

		private void SetMemoryDomains()
		{
			var mm = new List<MemoryDomain>();

			for (int i = 0; i < _numCores; i++)
			{
				foreach (var md in _linkedCores[i].AsMemoryDomains() as MemoryDomainList)
				{
					mm.Add(new WrappedMemoryDomain($"P{i + 1} " + md.Name, md));
				}
			}

			_memoryDomains = new MemoryDomainList(mm);
			_serviceProvider.Register<IMemoryDomains>(_memoryDomains);
		}

		private class WrappedMemoryDomain : MemoryDomain
		{
			private readonly MemoryDomain _m;

			public WrappedMemoryDomain(string name, MemoryDomain m)
			{
				Name = name;
				Size = m.Size;
				WordSize = m.WordSize;
				EndianType = m.EndianType;
				Writable = m.Writable;

				_m = m;
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
