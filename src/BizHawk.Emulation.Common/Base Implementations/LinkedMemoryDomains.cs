#nullable disable

using System.Collections.Generic;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// A generic implementation of IMemoryDomains that can be used by any link core
	/// </summary>
	/// <seealso cref="IMemoryDomains" />
	public class LinkedMemoryDomains : MemoryDomainList
	{
		public LinkedMemoryDomains(IEmulator[] linkedCores, int numCores)
			: base(LinkMemoryDomains(linkedCores, numCores))
		{
			SystemBus = linkedCores[0].AsMemoryDomains().SystemBus;
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
