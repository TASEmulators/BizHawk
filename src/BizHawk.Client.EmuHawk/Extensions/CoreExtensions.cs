using System.Drawing;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Nintendo.SNES9X;
using BizHawk.Emulation.Cores.Consoles.Sega.gpgx;
using BizHawk.Emulation.Cores.Arcades.MAME;
using BizHawk.Emulation.Cores.Consoles.Nintendo.NDS;
using BizHawk.Emulation.Cores.Nintendo.GBA;
using BizHawk.Emulation.Cores.Sega.MasterSystem;

namespace BizHawk.Client.EmuHawk.CoreExtensions
{
	public static class CoreExtensions
	{
		public static Bitmap Icon(this IEmulator core)
		{
			var attributes = core.Attributes();

			if (!attributes.Ported)
			{
				return Properties.Resources.CorpHawkSmall;
			}

			return core switch
			{
				QuickNES => Properties.Resources.QuickNes,
				LibsnesCore => Properties.Resources.Bsnes,
				GPGX => Properties.Resources.GenPlus,
				Gameboy => Properties.Resources.Gambatte,
				Snes9x => Properties.Resources.Snes9X,
				MAME => Properties.Resources.Mame,
				MGBAHawk => Properties.Resources.Mgba,
				MelonDS => Properties.Resources.MelonDS,
				_ => null
			};
		}

		public static string DisplayName(this IEmulator core)
		{
			var attributes = core.Attributes();

			var str = (!attributes.Released ? "(Experimental) " : "") +
				attributes.CoreName;

			return str;
		}

		public static string GetSystemDisplayName(this IEmulator emulator) => emulator.SystemId switch
		{
			"NULL" => string.Empty,
			"NES" => "NES",
			"INTV" => "Intellivision",
			"SG" => "SG-1000",
			"SMS" when emulator is SMS { IsGameGear: true } => "Game Gear",
			"SMS" when emulator is SMS { IsSG1000: true } => "SG-1000",
			"SMS" => "Sega Master System",
			"PCECD" => "TurboGrafx - 16(CD)",
			"PCE" => "TurboGrafx-16",
			"SGX" => "SuperGrafx",
			"GEN" => "Genesis",
			"TI83" => "TI - 83",
			"SNES" => "SNES",
#if false
			"GB" when emulator is IGameboyCommon gb && gb.IsCGBMode() => "Gameboy Color",
#endif
			"GB" => "GB",
			"A26" => "Atari 2600",
			"A78" => "Atari 7800",
			"C64" => "Commodore 64",
			"Coleco" => "ColecoVision",
			"GBA" => "Gameboy Advance",
			"NDS" => "NDS",
			"N64" => "Nintendo 64",
			"SAT" => "Saturn",
			"DGB" => "Game Boy Link",
			"GB3x" => "Game Boy Link 3x",
			"GB4x" => "Game Boy Link 4x",
			"WSWAN" => "WonderSwan",
			"Lynx" => "Lynx",
			"PSX" => "PlayStation",
			"AppleII" => "Apple II",
			"Libretro" => "Libretro",
			"VB" => "Virtual Boy",
			"VEC" => "Vectrex",
			"NGP" => "Neo-Geo Pocket",
			"ZXSpectrum" => "ZX Spectrum",
			"AmstradCPC" => "Amstrad CPC",
			"ChannelF" => "Channel F",
			"O2" => "Odyssey2",
			"MAME" => "MAME",
			"uzem" => "uzem",
			"PCFX" => "PCFX",
			_ => string.Empty
		};
	}
}
