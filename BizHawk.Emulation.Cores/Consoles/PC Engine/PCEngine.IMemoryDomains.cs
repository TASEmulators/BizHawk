using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.PCEngine
{
	public sealed partial class PCEngine
	{
		private Dictionary<string, MemoryDomainByteArray> _byteArrayDomains = new Dictionary<string, MemoryDomainByteArray>();
		private bool _memoryDomainsInit = false;
		private MemoryDomainList _memoryDomains;

		private void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>(2);

			var SystemBusDomain = new MemoryDomainDelegate("System Bus (21 bit)", 0x200000, MemoryDomain.Endian.Little,
				(addr) =>
				{
					if (addr < 0 || addr >= 0x200000)
						throw new ArgumentOutOfRangeException();
					return Cpu.ReadMemory21((int)addr);
				},
				(addr, value) =>
				{
					if (addr < 0 || addr >= 0x200000)
						throw new ArgumentOutOfRangeException();
					Cpu.WriteMemory21((int)addr, value);
				},
				wordSize: 2);
			domains.Add(SystemBusDomain);

			var CpuBusDomain = new MemoryDomainDelegate("System Bus", 0x10000, MemoryDomain.Endian.Little,
				(addr) =>
				{
					if (addr < 0 || addr >= 0x10000)
						throw new ArgumentOutOfRangeException();
					return Cpu.ReadMemory((ushort)addr);
				},
				(addr, value) =>
				{
					if (addr < 0 || addr >= 0x10000)
						throw new ArgumentOutOfRangeException();
					Cpu.WriteMemory((ushort)addr, value);
				},
				wordSize: 2);
			domains.Add(CpuBusDomain);

			SyncAllByteArrayDomains();

			_memoryDomains = new MemoryDomainList(domains.Concat(_byteArrayDomains.Values).ToList());
			_memoryDomains.SystemBus = CpuBusDomain;
			_memoryDomains.MainMemory = _byteArrayDomains["Main Memory"];
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(_memoryDomains);
			_memoryDomainsInit = true;
		}

		private void SyncAllByteArrayDomains()
		{
			SyncByteArrayDomain("Main Memory", Ram);
			SyncByteArrayDomain("ROM", RomData);

			if (BRAM != null)
				SyncByteArrayDomain("Battery RAM", BRAM);

			if (TurboCD)
			{
				SyncByteArrayDomain("TurboCD RAM", CDRam);
				SyncByteArrayDomain("ADPCM RAM", ADPCM.RAM);
				if (SuperRam != null)
				{
					SyncByteArrayDomain("Super System Card RAM", SuperRam);
				}
			}

			if (ArcadeCard)
			{
				SyncByteArrayDomain("Arcade Card RAM", ArcadeRam);
			}

			if (PopulousRAM != null)
			{
				SyncByteArrayDomain("Cart Battery RAM", PopulousRAM);
			}
		}

		private void SyncByteArrayDomain(string name, byte[] data)
		{
			if (_memoryDomainsInit)
			{
				var m = _byteArrayDomains[name];
				m.Data = data;
			}
			else
			{
				var m = new MemoryDomainByteArray(name, MemoryDomain.Endian.Little, data, true, 1);
				_byteArrayDomains.Add(name, m);
			}
		}
	}
}
