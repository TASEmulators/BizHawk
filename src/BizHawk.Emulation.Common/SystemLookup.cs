using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Emulation.Common
{
	// TODO: This should build itself from the Cores assembly, we don't want to maintain this
	public class SystemLookup
	{
		private readonly List<SystemInfo> _systems = new List<SystemInfo>
		{
			new SystemInfo("A26", "Atari 2600"),
			new SystemInfo("A78", "Atari 7800"),
			new SystemInfo("Lynx", "Atari Lynx"),
			new SystemInfo("NES", "NES"),
			new SystemInfo("SNES", "Super NES"),
			new SystemInfo("N64", "Nintendo 64"),
			new SystemInfo("GB", "Gameboy"),
			new SystemInfo("GBA", "Gameboy Advance"),
			new SystemInfo("PSX", "Playstation"),
			new SystemInfo("SMS", "Sega Master System"),
			new SystemInfo("GEN", "Sega Genesis/Mega Drive"),
			new SystemInfo("32X", "Sega Genesis 32X/Mega Drive 32X"),
			new SystemInfo("SAT", "Sega Saturn"),
			new SystemInfo("PCE", "PC Engine/TurboGrafx 16"),
			new SystemInfo("Coleco", "ColecoVision"),
			new SystemInfo("TI83", "TI-83 Calculator"),
			new SystemInfo("WSWAN", "WonderSwan"),
			new SystemInfo("C64", "Commodore 64"),
			new SystemInfo("AppleII", "Apple II"),
			new SystemInfo("INTV", "IntelliVision"),
			new SystemInfo("ZXSpectrum", "Sinclair ZX Spectrum"),
			new SystemInfo("AmstradCPC",  "Amstrad CPC"),
			new SystemInfo("ChannelF",  "Fairchild Channel F"),
			new SystemInfo("O2", "Odyssey2"),
			new SystemInfo("VEC", "Vectrex"),
			new SystemInfo("MSX", "MSX"),
			new SystemInfo("NDS", "Nintendo DS")
		};

		public SystemInfo this[string systemId]
			=> _systems.FirstOrDefault(s => s.SystemId == systemId)
			?? new SystemInfo("Unknown", "Unknown");

		public IEnumerable<SystemInfo> AllSystems => _systems;

		public class SystemInfo
		{
			public SystemInfo(string systemId, string fullName)
			{
				SystemId = systemId;
				FullName = fullName;
			}

			public string SystemId { get; }
			public string FullName { get; }
		}
	}
}
