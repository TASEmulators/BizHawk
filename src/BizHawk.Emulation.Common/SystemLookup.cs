using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Emulation.Common
{
	// TODO: This should build itself from the Cores assembly, we don't want to maintain this
	public class SystemLookup
	{
		private readonly List<SystemInfo> _systems = new List<SystemInfo>
		{
			new(VSystemID.Raw.A26, "Atari 2600"),
			new(VSystemID.Raw.A78, "Atari 7800"),
			new(VSystemID.Raw.Lynx, "Atari Lynx"),
			new(VSystemID.Raw.NES, "NES"),
			new(VSystemID.Raw.SNES, "Super NES"),
			new(VSystemID.Raw.N64, "Nintendo 64"),
			new(VSystemID.Raw.GB, "Gameboy"),
			new(VSystemID.Raw.GBA, "Gameboy Advance"),
			new(VSystemID.Raw.PSX, "Playstation"),
			new(VSystemID.Raw.SMS, "Sega Master System"),
			new(VSystemID.Raw.GEN, "Sega Genesis/Mega Drive"),
			new(VSystemID.Raw.Sega32X, "Sega Genesis 32X/Mega Drive 32X"),
			new(VSystemID.Raw.SAT, "Sega Saturn"),
			new(VSystemID.Raw.PCE, "PC Engine/TurboGrafx 16"),
			new(VSystemID.Raw.Coleco, "ColecoVision"),
			new(VSystemID.Raw.TI83, "TI-83 Calculator"),
			new(VSystemID.Raw.WSWAN, "WonderSwan"),
			new(VSystemID.Raw.C64, "Commodore 64"),
			new(VSystemID.Raw.AppleII, "Apple II"),
			new(VSystemID.Raw.INTV, "IntelliVision"),
			new(VSystemID.Raw.ZXSpectrum, "Sinclair ZX Spectrum"),
			new(VSystemID.Raw.AmstradCPC,  "Amstrad CPC"),
			new(VSystemID.Raw.ChannelF,  "Fairchild Channel F"),
			new(VSystemID.Raw.O2, "Odyssey2"),
			new(VSystemID.Raw.VEC, "Vectrex"),
			new(VSystemID.Raw.MSX, "MSX"),
			new(VSystemID.Raw.NDS, "Nintendo DS")
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
