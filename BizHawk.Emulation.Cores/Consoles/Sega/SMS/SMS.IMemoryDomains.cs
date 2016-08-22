using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public sealed partial class SMS
	{
		private MemoryDomainList MemoryDomains;
		private readonly Dictionary<string, MemoryDomainByteArray> _byteArrayDomains = new Dictionary<string, MemoryDomainByteArray>();
		private bool _memoryDomainsInit = false;

		void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>
			{
				new MemoryDomainDelegate("System Bus", 0x10000, MemoryDomain.Endian.Little,
				(addr) =>
				{
					if (addr < 0 || addr >= 65536)
					{
						throw new ArgumentOutOfRangeException();
					}

					return Cpu.ReadMemory((ushort)addr);
				},
				(addr, value) =>
				{
					if (addr < 0 || addr >= 65536)
					{
						throw new ArgumentOutOfRangeException();
					}

					Cpu.WriteMemory((ushort)addr, value);
				}, 1)
			};

			if (SaveRAM != null)
			{
				var saveRamDomain = new MemoryDomainDelegate("Save RAM", SaveRAM.Length, MemoryDomain.Endian.Little,
					addr => SaveRAM[addr],
					(addr, value) => { SaveRAM[addr] = value; SaveRamModified = true; }, 1);
				domains.Add(saveRamDomain);
			}

			SyncAllByteArrayDomains();

			MemoryDomains = new MemoryDomainList(_byteArrayDomains.Values.Concat(domains).ToList());
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(MemoryDomains);

			_memoryDomainsInit = true;
		}

		private void SyncAllByteArrayDomains()
		{
			SyncByteArrayDomain("Main RAM", SystemRam);
			SyncByteArrayDomain("Video RAM", Vdp.VRAM);
			SyncByteArrayDomain("ROM", RomData);

			if (ExtRam != null)
			{
				SyncByteArrayDomain("Cart (Volatile) RAM", Vdp.VRAM);
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
