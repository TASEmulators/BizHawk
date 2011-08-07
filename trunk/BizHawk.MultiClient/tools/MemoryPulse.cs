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
			public MemoryDomain domain;
		}

		private static List<MemoryPulseEntry> entries = new List<MemoryPulseEntry>();

		public static void Pulse()
		{
			foreach (var entry in entries)
			{
				entry.domain.PokeByte(entry.address, entry.value);
			}
		}

		public static void Add(MemoryDomain domain, int address, byte value)
		{
			entries.RemoveAll(o => o.domain == domain && o.address == address);

			var entry = new MemoryPulseEntry { address = address, value = value, domain = domain };
			entries.Add(entry);
		}

		public static MemoryPulseEntry Query(MemoryDomain domain, int address)
		{
			return entries.Find(o => o.domain == domain && o.address == address);
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
