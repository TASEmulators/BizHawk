using System;
using System.Collections.Generic;

namespace BizHawk.MultiClient
{
	public static class MemoryPulse
	{
		public class MemoryPulseEntry
		{
			public int address;
			public byte value;
			public byte? compare;
			public MemoryDomain domain;
		}

		private static List<MemoryPulseEntry> entries = new List<MemoryPulseEntry>();

		public static void Pulse()
		{
			foreach (var entry in entries)
			{
				if (entry.compare.HasValue)
				{
					if (entry.domain.PeekByte(entry.address) == entry.compare)
					{
						entry.domain.PokeByte(entry.address, entry.value);
					}
				}
				else
				{
					entry.domain.PokeByte(entry.address, entry.value);
				}
			}
		}

		public static void Add(MemoryDomain domain, int address, byte value, byte? compare = null)
		{
			entries.RemoveAll(o => o.domain == domain && o.address == address);

			var entry = new MemoryPulseEntry { address = address, value = value, domain = domain };
			entries.Add(entry);
		}

		public static MemoryPulseEntry Query(MemoryDomain domain, int address, byte? compare = null)
		{
			if (compare.HasValue)
			{
				return entries.Find(o => o.domain == domain && o.address == address && o.compare == compare);
			}
			else
			{
				return entries.Find(o => o.domain == domain && o.address == address);
			}
		}

		public static void Remove(MemoryDomain domain, int address)
		{
			entries.RemoveAll(o => o.domain == domain && o.address == address);
		}

		public static IList<MemoryPulseEntry> GetEntries()
		{
			return entries.AsReadOnly();
		}

		public static void Clear()
		{
			entries.Clear();
		}
	}
}
