using System;
using System.Collections.Generic;
using System.Linq;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common.RamSearchEngine
{
	internal static class Extensions
	{
		public static float ToFloat(this long val)
		{
			var bytes = BitConverter.GetBytes((int)val);
			return BitConverter.ToSingle(bytes, 0);
		}

		public static IEnumerable<IMiniWatch> ToBytes(this IEnumerable<long> addresses, SearchEngineSettings settings)
			=> settings.IsDetailed()
				? addresses.ToBytes(settings.Domain)
				: addresses.ToDetailedBytes(settings.Domain);

		public static IEnumerable<IMiniWatch> ToWords(this IEnumerable<long> addresses, SearchEngineSettings settings)
			=> settings.IsDetailed()
				? addresses.ToWords(settings.Domain, settings.BigEndian)
				: addresses.ToDetailedWords(settings.Domain, settings.BigEndian);

		public static IEnumerable<IMiniWatch> ToDWords(this IEnumerable<long> addresses, SearchEngineSettings settings)
			=> settings.IsDetailed()
				? addresses.ToDWords(settings.Domain, settings.BigEndian)
				: addresses.ToDetailedDWords(settings.Domain, settings.BigEndian);

		private static IEnumerable<IMiniWatch> ToBytes(this IEnumerable<long> addresses, MemoryDomain domain)
			=> addresses.Select(a => new MiniByteWatch(domain, a));

		private static IEnumerable<IMiniWatch> ToDetailedBytes(this IEnumerable<long> addresses, MemoryDomain domain)
			=> addresses.Select(a => new MiniByteWatchDetailed(domain, a));

		private static IEnumerable<IMiniWatch> ToWords(this IEnumerable<long> addresses, MemoryDomain domain, bool bigEndian)
			=> addresses.Select(a => new MiniWordWatch(domain, a, bigEndian));

		private static IEnumerable<IMiniWatch> ToDetailedWords(this IEnumerable<long> addresses, MemoryDomain domain, bool bigEndian)
			=> addresses.Select(a => new MiniWordWatchDetailed(domain, a, bigEndian));

		private static IEnumerable<IMiniWatch> ToDWords(this IEnumerable<long> addresses, MemoryDomain domain, bool bigEndian)
			=> addresses.Select(a => new MiniDWordWatch(domain, a, bigEndian));

		private static IEnumerable<IMiniWatch> ToDetailedDWords(this IEnumerable<long> addresses, MemoryDomain domain, bool bigEndian)
			=> addresses.Select(a => new MiniDWordWatchDetailed(domain, a, bigEndian));
	}
}
