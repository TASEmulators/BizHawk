using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	unsafe partial class MelonDS : IMemoryDomains
	{
		SortedList<string, MemoryDomain> domains;

		private void InitMemoryDomains()
		{
			_mainMemory = new MemoryDomainIntPtr("RAM", MemoryDomain.Endian.Little, (IntPtr)GetMainMemory(), GetMainMemorySize(), true, 4);

			domains = new SortedList<string, MemoryDomain>();
			domains.Add("RAM", _mainMemory);
		}

		public MemoryDomain this[string name] => domains[name];
		public bool Has(string name)
		{
			return domains.ContainsKey(name);
		}

		private MemoryDomain _mainMemory;
		public MemoryDomain MainMemory { get => _mainMemory; set => throw new NotImplementedException(); }

		[DllImport(dllPath)]
		private static extern byte* GetMainMemory();
		[DllImport(dllPath)]
		private static extern int GetMainMemorySize();

		// NDS has two CPUs, with different memory mappings.
		public bool HasSystemBus => false;
		public MemoryDomain SystemBus { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public IEnumerator<MemoryDomain> GetEnumerator()
		{
			return domains.Values.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}
}
