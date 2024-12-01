#nullable disable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// A generic implementation of IMemoryDomain that can be used by any core
	/// </summary>
	/// <seealso cref="IMemoryDomains" />
	public class MemoryDomainList : ReadOnlyCollection<MemoryDomain>, IMemoryDomains
	{
		private MemoryDomain _mainMemory;
		private MemoryDomain _systemBus;

		public bool Has(string name)
		{
			return this.Any(md => md.Name == name);
		}

		public MemoryDomainList(IList<MemoryDomain> domains)
			: base(domains)
		{
		}

		public MemoryDomain this[string name] => this.FirstOrDefault(x => x.Name == name);

		public MemoryDomain MainMemory
		{
			get => _mainMemory ?? this[0];
			set => _mainMemory = value;
		}

		public bool HasSystemBus => _systemBus != null || this.Any(x => x.Name == "System Bus");

		public MemoryDomain SystemBus
		{
			get
			{
				if (_systemBus != null)
				{
					return _systemBus;
				}

				return this.FirstOrDefault(x => x.Name == "System Bus") ?? MainMemory;
			}

			set => _systemBus = value;
		}

		/// <summary>
		/// for core use only
		/// </summary>
		public void MergeList(MemoryDomainList other)
		{
			var domains = this.ToDictionary(m => m.Name);
			foreach (var src in other)
			{
				if (domains.TryGetValue(src.Name, out var dst))
				{
					TryMerge<MemoryDomainByteArray>(dst, src, (d, s) => d.Data = s.Data);
					TryMerge<MemoryDomainIntPtr>(dst, src, (d, s) => d.Data = s.Data);
					TryMerge<MemoryDomainIntPtrSwap16>(dst, src, (d, s) => d.Data = s.Data);
					TryMerge<MemoryDomainDelegate>(dst, src, (d, s) => { d.Peek = s.Peek; d.Poke = s.Poke; });
				}
			}
		}

		/// <summary>
		/// big hacks
		/// </summary>
		/// <typeparam name="T">The memory domain type to merge</typeparam>
		private static void TryMerge<T>(MemoryDomain dest, MemoryDomain src, Action<T, T> func)
			where T : MemoryDomain
		{
			if (dest is T d1 && src is T s1)
			{
				func(d1, s1);
			}
		}
	}
}
