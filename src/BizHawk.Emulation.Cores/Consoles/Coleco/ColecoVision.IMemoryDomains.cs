using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.ColecoVision
{
	public partial class ColecoVision
	{
		private MemoryDomainList _memoryDomains;
		private readonly Dictionary<string, MemoryDomainByteArray> _byteArrayDomains = new Dictionary<string, MemoryDomainByteArray>();
		private bool _memoryDomainsInit = false;

		private void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>
			{
				new MemoryDomainDelegate("System Bus", 0x10000, MemoryDomain.Endian.Little,
				addr =>
				{
					if (addr < 0 || addr >= 65536)
					{
						throw new ArgumentOutOfRangeException();
					}

					return _cpu.ReadMemory((ushort)addr);
				},
				(addr, value) =>
				{
					if (addr < 0 || addr >= 65536)
					{
						throw new ArgumentOutOfRangeException();
					}

					_cpu.WriteMemory((ushort)addr, value);
				}, 1)
			};

			if (use_SGM)
			{
				var SGMLRam = new MemoryDomainByteArray("SGM Low RAM", MemoryDomain.Endian.Little, SGM_low_RAM, true, 1);
				domains.Add(SGMLRam);

				var SGMHRam = new MemoryDomainByteArray("SGM High RAM", MemoryDomain.Endian.Little, SGM_high_RAM, true, 1);
				domains.Add(SGMHRam);
			}

			SyncAllByteArrayDomains();

			_memoryDomains = new MemoryDomainList(_byteArrayDomains.Values.Concat(domains).ToList());
			((BasicServiceProvider)ServiceProvider).Register<IMemoryDomains>(_memoryDomains);

			_memoryDomainsInit = true;
		}

		private void SyncAllByteArrayDomains()
		{
			SyncByteArrayDomain("Main RAM", _ram);
			SyncByteArrayDomain("Video RAM", _vdp.VRAM);
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
