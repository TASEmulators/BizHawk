using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.PCEngine
{
	public sealed partial class PCEngine
	{
		private readonly Dictionary<string, MemoryDomainByteArray> _byteArrayDomains = new();
		private readonly Dictionary<string, MemoryDomainUshortArray> _ushortArrayDomains = new();
		private bool _memoryDomainsInit;
		private MemoryDomainList _memoryDomains;

		private void SetupMemoryDomains()
		{
			List<MemoryDomain> domains = new();

			MemoryDomainDelegate systemBusDomain = new("System Bus (21 bit)", 0x200000, MemoryDomain.Endian.Little,
				(addr) =>
				{
					if (addr is < 0 or > 0x1FFFFF) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
					return Cpu.ReadMemory21((int)addr);
				},
				(addr, value) =>
				{
					if (addr is < 0 or > 0x1FFFFF) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
					Cpu.WriteMemory21((int)addr, value);
				},
				wordSize: 2);
			domains.Add(systemBusDomain);

			MemoryDomainDelegate cpuBusDomain = new("System Bus", 0x10000, MemoryDomain.Endian.Little,
				(addr) =>
				{
					if (addr is < 0 or > 0xFFFF) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
					return Cpu.PeekMemory((ushort)addr);
				},
				(addr, value) =>
				{
					if (addr is < 0 or > 0xFFFF) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
					Cpu.PokeMemory((ushort)addr, value);
				},
				wordSize: 2);
			domains.Add(cpuBusDomain);

			SyncAllByteArrayDomains();

			domains.AddRange(_byteArrayDomains.Values);
			domains.AddRange(_ushortArrayDomains.Values);

			_memoryDomains = new MemoryDomainList(domains)
			{
				SystemBus = cpuBusDomain,
				MainMemory = _byteArrayDomains["Main Memory"]
			};

			((BasicServiceProvider) ServiceProvider).Register<IMemoryDomains>(_memoryDomains);
			_memoryDomainsInit = true;
		}

		private void SyncAllByteArrayDomains()
		{
			SyncByteArrayDomain("Main Memory", Ram);
			SyncByteArrayDomain("ROM", RomData);

			SyncUshortArrayDomain("VRAM1", VDC1.VRAM);

			SyncUshortArrayDomain("VCEPalette", VCE.VceData);

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
				MemoryDomainByteArray m = new(name, MemoryDomain.Endian.Little, data, true, 1);
				_byteArrayDomains.Add(name, m);
			}
		}

		private void SyncUshortArrayDomain(string name, ushort[] data)
		{
			if (_memoryDomainsInit)
			{
				var m = _ushortArrayDomains[name];
				m.Data = data;
			}
			else
			{
				MemoryDomainUshortArray m = new(name, MemoryDomain.Endian.Big, data, true);
				_ushortArrayDomains.Add(name, m);
			}
		}
	}
}
