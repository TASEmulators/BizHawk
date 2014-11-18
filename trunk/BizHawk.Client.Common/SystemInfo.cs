using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class SystemInfo
	{
		public SystemInfo() { }

		public string DisplayName { get; set; }
		public int ByteSize { get; set; } // For Ram tools, whether it is a 8/16/32 bit system

		public static SystemInfo Null
		{
			get
			{
				return new SystemInfo
				{
					DisplayName = "",
					ByteSize = 1,
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
					ByteSize = 1,
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
					ByteSize = 2,
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
					ByteSize = 1,
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
					ByteSize = 1,
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
					ByteSize = 1,
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
					ByteSize = 2,
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
					ByteSize = 2,
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
					ByteSize = 2,
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
					ByteSize = 2,
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
					ByteSize = 1,
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
					ByteSize = 2,
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
					ByteSize = 1,
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
					ByteSize = 1,
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
					ByteSize = 1,
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
					ByteSize = 1,
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
					ByteSize = 1,
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
					ByteSize = 1,
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
					ByteSize = 4,
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
					ByteSize = 4,
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
					ByteSize = 4,
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
					ByteSize = 1,
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
					ByteSize = 1,
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
					ByteSize = 2,
				};
			}
		}
	}
}
