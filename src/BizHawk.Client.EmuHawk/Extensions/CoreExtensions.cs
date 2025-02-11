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
using BizHawk.Emulation.Cores.Computers.Amiga;

namespace BizHawk.Client.EmuHawk.CoreExtensions
{
	public static class CoreExtensions
	{
		public static Bitmap Icon(this IEmulator core)
		{
			var attributes = core.Attributes();

			if (attributes is not PortedCoreAttribute)
			{
				return Properties.Resources.CorpHawkSmall;
			}

			// (select) cores A-Z by value of `CoreAttribute.CoreName`
			return core switch
			{
				LibsnesCore => Properties.Resources.Bsnes,
				Gameboy => Properties.Resources.Gambatte,
				GPGX => Properties.Resources.GenPlus,
				MAME => Properties.Resources.Mame,
				NDS => Properties.Resources.MelonDS,
				MGBAHawk => Properties.Resources.Mgba,
				QuickNES => Properties.Resources.QuickNes,
				Snes9x => Properties.Resources.Snes9X,
				UAE => Properties.Resources.Amiga,
				_ => null
			};
		}

		public static string GetSystemDisplayName(this IEmulator emulator) => emulator switch
		{
			NullEmulator => string.Empty,
#if false
			IGameboyCommon gb when gb.IsCGBMode() => EmulatorExtensions.SystemIDToDisplayName(VSystemID.Raw.GBC),
#endif
			_ => EmulatorExtensions.SystemIDToDisplayName(emulator.SystemId)
		};
	}
}
