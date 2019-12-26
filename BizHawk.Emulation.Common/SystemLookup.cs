using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Emulation.Common
{
	// TODO: This should build itself from the Cores assembly, we don't want to maintain this
	public class SystemLookup
	{
		private readonly List<SystemInfo> _systems = new List<SystemInfo>
		{
			new SystemInfo { SystemId = "A26", FullName = "Atari 2600" },
			new SystemInfo { SystemId = "A78", FullName = "Atari 7800" },
			new SystemInfo { SystemId = "Lynx", FullName = "Atari Lynx" },

			new SystemInfo { SystemId = "NES", FullName = "NES" },
			new SystemInfo { SystemId = "SNES", FullName = "Super NES" },
			new SystemInfo { SystemId = "N64", FullName = "Nintendo 64" },

			new SystemInfo { SystemId = "GB", FullName = "Gameboy" },
			new SystemInfo { SystemId = "GBA", FullName = "Gameboy Advance" },

			new SystemInfo { SystemId = "PSX", FullName = "Playstation" },

			new SystemInfo { SystemId = "SMS", FullName = "Sega Master System" },
			new SystemInfo { SystemId = "GEN", FullName = "Sega Genesis/Mega Drive" },
			new SystemInfo { SystemId = "32X", FullName = "Sega Genesis 32X/Mega Drive 32X" },
			new SystemInfo { SystemId = "SAT", FullName = "Sega Saturn" },

			new SystemInfo { SystemId = "PCE", FullName = "PC Engine/TurboGrafx 16" },
			new SystemInfo { SystemId = "Coleco", FullName = "ColecoVision" },
			new SystemInfo { SystemId = "TI83", FullName = "TI-83 Calculator" },
			new SystemInfo { SystemId = "WSWAN", FullName = "WonderSwan" },

			new SystemInfo { SystemId = "C64", FullName = "Commodore 64" },
			new SystemInfo { SystemId = "AppleII", FullName = "Apple II" },
			new SystemInfo { SystemId = "INTV", FullName = "IntelliVision" },
			new SystemInfo { SystemId = "ZXSpectrum", FullName = "Sinclair ZX Spectrum" },
			new SystemInfo { SystemId = "AmstradCPC", FullName = "Amstrad CPC" },
			new SystemInfo { SystemId = "ChannelF", FullName = "Fairchild Channel F"},
			new SystemInfo { SystemId = "O2", FullName = "Odyssey2"},
			new SystemInfo { SystemId = "VEC", FullName = "Vectrex"}
		};

		public SystemInfo this[string systemId]
		{
			get
			{
				var system = _systems.FirstOrDefault(s => s.SystemId == systemId);
				return system ?? new SystemInfo { SystemId = "Unknown", FullName = "Unknown" };
			}
		}

		public IEnumerable<SystemInfo> AllSystems => _systems;

		public class SystemInfo
		{
			public string SystemId { get; set; }
			public string FullName { get; set; }
		}
	}
}
