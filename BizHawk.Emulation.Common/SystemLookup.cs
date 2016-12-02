using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BizHawk.Emulation.Common
{
	// TODO: This should build itself from the Cores assembly, we don't want to maintain this
	public class SystemLookup
	{
		private readonly List<SystemInfo> Systems = new List<SystemInfo>
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
			new SystemInfo { SystemId = "GEN", FullName = "Sega Genesis/Megadrive" },
			new SystemInfo { SystemId = "SAT", FullName = "Sega Saturn" },

			new SystemInfo { SystemId = "PCE", FullName = "PC Engine/TurboGrafx 16" },
			new SystemInfo { SystemId = "Coleco", FullName = "Colecovision" },
			new SystemInfo { SystemId = "TI83", FullName = "TI-83 Calculator" },
			new SystemInfo { SystemId = "WSWAN", FullName = "WonderSwan" },

			new SystemInfo { SystemId = "C64", FullName = "Commodore 64" },
			new SystemInfo { SystemId = "AppleII", FullName = "Apple II" },
			new SystemInfo { SystemId = "INTV", FullName = "Intellivision" }
		};

		public SystemInfo this[string systemId]
		{
			get
			{
				var system = Systems.FirstOrDefault(s => s.SystemId == systemId);

				if (system != null)
				{
					return system;
				}

				return new SystemInfo { SystemId = "Unknown", FullName = "Unknown" };
			}
		}

		public IEnumerable<SystemInfo> AllSystems
		{
			get
			{
				if (VersionInfo.DeveloperBuild)
				{
					return Systems;
				}

				return Systems.Where(s => s.SystemId != "C64");
			}
		}

		public class SystemInfo
		{
			public string SystemId { get; set; }
			public string FullName { get; set; }
		}
	}
}
