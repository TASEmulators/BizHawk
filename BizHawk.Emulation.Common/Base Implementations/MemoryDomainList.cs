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
	}
}
