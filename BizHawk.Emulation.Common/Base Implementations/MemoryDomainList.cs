using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace BizHawk.Emulation.Common
{
	public class MemoryDomainList : ReadOnlyCollection<MemoryDomain>, IMemoryDomains
	{
		private MemoryDomain _mainMemory;
		private MemoryDomain _systemBus;

		public bool Has(string name)
		{
			return this.FirstOrDefault((md) => md.Name == name) != null;
		}

		public MemoryDomainList(IList<MemoryDomain> domains)
			: base(domains)
		{
		}

		public MemoryDomain this[string name]
		{
			get
			{
				return this.FirstOrDefault(x => x.Name == name);
			}
		}

		public MemoryDomain MainMemory
		{
			get
			{
				if (_mainMemory != null)
				{
					return _mainMemory;
				}

				return this.First();
			}

			set
			{
				_mainMemory = value;
			}
		}

		public bool HasSystemBus
		{
			get
			{
				if (_systemBus != null)
				{
					return true;
				}

				return this.Any(x => x.Name == "System Bus");
			}
		}

		public MemoryDomain SystemBus
		{
			get
			{
				if (_systemBus != null)
				{
					return _systemBus;
				}

				var bus = this.FirstOrDefault(x => x.Name == "System Bus");

				if (bus != null)
				{
					return bus;
				}

				return MainMemory;
			}

			set
			{
				_systemBus = value;
			}
		}

		/// <summary>
		/// for core use only
		/// </summary>
		public void MergeList(MemoryDomainList other)
		{
			var domains = this.ToDictionary(m => m.Name);
			foreach (var src in other)
			{
				MemoryDomain dst;
				if (domains.TryGetValue(src.Name, out dst))
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
		private static void TryMerge<T>(MemoryDomain dest, MemoryDomain src, Action<T, T> func)
			where T : MemoryDomain
		{
			var d1 = dest as T;
			var s1 = src as T;
			if (d1 != null && s1 != null)
				func(d1, s1);
		}
	}
}
