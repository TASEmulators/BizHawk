using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	unsafe partial class MelonDS : IMemoryDomains
	{
		SortedList<string, MemoryDomain> domains;

		private void InitMemoryDomains()
		{
			MainMemory = new MemoryDomainIntPtr("RAM", MemoryDomain.Endian.Little, (IntPtr)GetMainMemory(), GetMainMemorySize(), true, 4);
			SystemBus = new MelonSystemBus();

			domains = new SortedList<string, MemoryDomain>();
			domains.Add("RAM", MainMemory);
			domains.Add("System Bus", SystemBus);
		}

		public MemoryDomain this[string name] => domains[name];
		public bool Has(string name)
		{
			return domains.ContainsKey(name);
		}

		public MemoryDomain MainMemory { get; private set; }

		[DllImport(dllPath)]
		private static extern byte* GetMainMemory();
		[DllImport(dllPath)]
		private static extern int GetMainMemorySize();

		// NDS has two CPUs, with different memory mappings.
		public bool HasSystemBus => true;
		public MemoryDomain SystemBus { get; private set; }
		private class MelonSystemBus : MemoryDomain
		{
			[DllImport(dllPath)]
			private static extern byte ARM9Read8(uint addr);
			[DllImport(dllPath)]
			private static extern void ARM9Write8(uint addr, byte value);
			[DllImport(dllPath)]
			private static extern ushort ARM9Read16(uint addr);
			[DllImport(dllPath)]
			private static extern void ARM9Write16(uint addr, ushort value);
			[DllImport(dllPath)]
			private static extern uint ARM9Read32(uint addr);
			[DllImport(dllPath)]
			private static extern void ARM9Write32(uint addr, uint value);

			public MelonSystemBus()
			{
				Name = "System Bus";
				Size = 0x0B00_0000;
				WordSize = 4;
				EndianType = Endian.Big;
				Writable = true;
			}

			public override byte PeekByte(long addr)
			{
				return ARM9Read8((uint)addr);
			}
			public override void PokeByte(long addr, byte val)
			{
				ARM9Write8((uint)addr, val);
			}
			public override ushort PeekUshort(long addr, bool bigEndian)
			{
				ushort ret = ARM9Read16((uint)addr);
				if (bigEndian)
					ret = SwapEndianness(ret);
				return ret;
			}
			public override void PokeUshort(long addr, ushort val, bool bigEndian)
			{
				if (bigEndian)
					val = SwapEndianness(val);
				ARM9Write16((uint)addr, val);
			}

			public override uint PeekUint(long addr, bool bigEndian)
			{
				uint ret = ARM9Read32((uint)addr);
				if (!bigEndian)
					ret = SwapEndianness(ret);
				return ret;

			}
			public override void PokeUint(long addr, uint val, bool bigEndian)
			{
				if (!bigEndian)
					val = SwapEndianness(val);
				ARM9Write32((uint)addr, val);
			}
		}

		public IEnumerator<MemoryDomain> GetEnumerator()
		{
			return domains.Values.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

		public static ushort SwapEndianness(ushort value)
		{
			return (ushort)((value >> 8) | (value << 8));
		}

		public static uint SwapEndianness(uint value)
		{
			return (value >> 24) | ((value & 0x00ff0000) >> 8) | ((value & 0x0000ff00) << 8) | (value << 24);
		}
	}
}
