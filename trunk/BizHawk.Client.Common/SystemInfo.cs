using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class SystemInfo
	{
		public SystemInfo() { }

		public string DisplayName { get; set; }

		public static SystemInfo Null
		{
			get
			{
				return new SystemInfo
				{
					DisplayName = "",
				};
			}
		}

		public static SystemInfo Nes
		{
			get
			{
				return new SystemInfo
				{
					DisplayName = "NES",
				};
			}
		}

		public static SystemInfo Intellivision
		{
			get
			{
				return new SystemInfo
				{
					DisplayName = "Intellivision",
				};
			}
		}

		public static SystemInfo SMS
		{
			get
			{
				return new SystemInfo
				{
					DisplayName = "Sega Master System",
				};
			}
		}

		public static SystemInfo SG
		{
			get
			{
				return new SystemInfo
				{
					DisplayName = "SG-1000",
				};
			}
		}

		public static SystemInfo GG
		{
			get
			{
				return new SystemInfo
				{
					DisplayName = "Game Gear",
				};
			}
		}

		public static SystemInfo PCE
		{
			get
			{
				return new SystemInfo
				{
					DisplayName = "TurboGrafx-16",
				};
			}
		}

		public static SystemInfo PCECD
		{
			get
			{
				return new SystemInfo
				{
					DisplayName = "TurboGrafx-16 (CD)",
				};
			}
		}

		public static SystemInfo SGX
		{
			get
			{
				return new SystemInfo
				{
					DisplayName = "SuperGrafx",
				};
			}
		}

		public static SystemInfo Genesis
		{
			get
			{
				return new SystemInfo
				{
					DisplayName = "Genesis",
				};
			}
		}

		public static SystemInfo TI83
		{
			get
			{
				return new SystemInfo
				{
					DisplayName = "TI-83",
				};
			}
		}

		public static SystemInfo SNES
		{
			get
			{
				return new SystemInfo
				{
					DisplayName = "SNES",
				};
			}
		}

		public static SystemInfo GB
		{
			get
			{
				return new SystemInfo
				{
					DisplayName = "Gameboy",
				};
			}
		}

		public static SystemInfo GBC
		{
			get
			{
				return new SystemInfo
				{
					DisplayName = "Gameboy Color",
				};
			}
		}

		public static SystemInfo Atari2600
		{
			get
			{
				return new SystemInfo
				{
					DisplayName = "Atari 2600",
				};
			}
		}

		public static SystemInfo Atari7800
		{
			get
			{
				return new SystemInfo
				{
					DisplayName = "Atari 7800",
				};
			}
		}

		public static SystemInfo C64
		{
			get
			{
				return new SystemInfo
				{
					DisplayName = "Commodore 64",
				};
			}
		}

		public static SystemInfo Coleco
		{
			get
			{
				return new SystemInfo
				{
					DisplayName = "ColecoVision",
				};
			}
		}

		public static SystemInfo GBA
		{
			get
			{
				return new SystemInfo
				{
					DisplayName = "Gameboy Advance",
				};
			}
		}

		public static SystemInfo N64
		{
			get
			{
				return new SystemInfo
				{
					DisplayName = "Nintendo 64",
				};
			}
		}

		public static SystemInfo Saturn
		{
			get
			{
				return new SystemInfo
				{
					DisplayName = "Saturn",
				};
			}
		}

		public static SystemInfo DualGB
		{
			get
			{
				return new SystemInfo
				{
					DisplayName = "Game Boy Link",
				};
			}
		}

		public static SystemInfo WonderSwan
		{
			get
			{
				return new SystemInfo
				{
					DisplayName = "WonderSwan",
				};
			}
		}
		public static SystemInfo Lynx
		{
			get
			{
				return new SystemInfo
				{
					DisplayName = "Lynx",
				};
			}
		}
		public static SystemInfo PSX
		{
			get
			{
				return new SystemInfo
				{
					DisplayName = "PlayStation",
				};
			}
		}
		public static SystemInfo AppleII
		{
			get
			{
				return new SystemInfo
				{
					DisplayName = "Apple II",
				};
			}
		}
	}
}
