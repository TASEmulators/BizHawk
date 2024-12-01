using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Libretro
{
	public partial class LibretroHost
	{
		private MemoryDomainList _memoryDomains;

		private static readonly IReadOnlyDictionary<LibretroApi.RETRO_MEMORY, string> _domainNames
			= new Dictionary<LibretroApi.RETRO_MEMORY, string>()
		{
			[LibretroApi.RETRO_MEMORY.SAVE_RAM] = "SaveRAM",
			[LibretroApi.RETRO_MEMORY.RTC] = "RTC",
			[LibretroApi.RETRO_MEMORY.SYSTEM_RAM] = "RAM",
			[LibretroApi.RETRO_MEMORY.VIDEO_RAM] = "VRAM",
		};

		private void InitMemoryDomains()
		{
			List<MemoryDomain> md = new();

			foreach (LibretroApi.RETRO_MEMORY m in Enum.GetValues(typeof(LibretroApi.RETRO_MEMORY)))
			{
				var mem = api.retro_get_memory_data(m);
				var sz = api.retro_get_memory_size(m);
				if (mem != IntPtr.Zero && sz > 0)
				{
					MemoryDomainIntPtr d = new(_domainNames[m], MemoryDomain.Endian.Unknown, mem, sz, true, 1);
					md.Add(d);

					if (m is LibretroApi.RETRO_MEMORY.SAVE_RAM or LibretroApi.RETRO_MEMORY.RTC)
					{
						_saveramAreas.Add(d);
						_saveramSize += d.Size;
					}
				}
			}

			// no domains to register...
			if (md.Count == 0)
			{
				return;
			}

			_memoryDomains = new(md);
			// if RAM is somehow not available, _memoryDomains["RAM"] just returns null (effective no-op)
			// what is considered main memory then is whatever was added first
			// priority implicitly then going to RETRO_MEMORY ordering (so SaveRAM->RTC->VRAM)
			_memoryDomains.MainMemory = _memoryDomains["RAM"];

			_serviceProvider.Register<IMemoryDomains>(_memoryDomains);
		}
	}
}
