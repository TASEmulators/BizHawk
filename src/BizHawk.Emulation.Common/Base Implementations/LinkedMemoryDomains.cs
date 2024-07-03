#nullable disable

using System.Collections.Generic;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// A generic linked implementation of IMemoryDomains that can be used by any link core
	/// </summary>
	/// <seealso cref="IMemoryDomains" />
	public class LinkedMemoryDomains : MemoryDomainList
	{
		public LinkedMemoryDomains(IEmulator[] linkedCores, int numCores, LinkedDisassemblable linkedDisassemblable)
			: base(LinkMemoryDomains(linkedCores, numCores))
		{
			if (linkedDisassemblable is not null)
			{
				SystemBus = new LinkedSystemBus(linkedCores, numCores, linkedDisassemblable);
			}
		}

		private static List<MemoryDomain> LinkMemoryDomains(IEmulator[] linkedCores, int numCores)
		{
			var mm = new List<MemoryDomain>();

			for (int i = 0; i < numCores; i++)
			{
				foreach (var md in linkedCores[i].AsMemoryDomains() as MemoryDomainList)
				{
					mm.Add(new WrappedMemoryDomain($"P{i + 1} " + md.Name, md));
				}
			}

			return mm;
		}

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

			public override byte PeekByte(long addr) => _m.PeekByte(addr);

			public override void PokeByte(long addr, byte val) => _m.PokeByte(addr, val);
		}

		private class LinkedSystemBus : MemoryDomain
		{
			private readonly MemoryDomain[] _linkedSystemBuses;
			private readonly LinkedDisassemblable _linkedDisassemblable;

			public LinkedSystemBus(IEmulator[] linkedCores, int numCores, LinkedDisassemblable linkedDisassemblable)
			{
				_linkedSystemBuses = new MemoryDomain[numCores];
				_linkedDisassemblable = linkedDisassemblable;
				for (int i = 0; i < numCores; i++)
				{
					_linkedSystemBuses[i] = linkedCores[i].AsMemoryDomains().SystemBus;
				}
				Name = "System Bus";
				Size = _linkedSystemBuses[0].Size;
				WordSize = _linkedSystemBuses[0].WordSize;
				EndianType = _linkedSystemBuses[0].EndianType;
				Writable = false;
			}

			public override byte PeekByte(long addr) => _linkedSystemBuses[int.Parse(_linkedDisassemblable.Cpu.Substring(1, 1)) - 1].PeekByte(addr);

			public override void PokeByte(long addr, byte val) => throw new NotImplementedException();
		}
	}
}
