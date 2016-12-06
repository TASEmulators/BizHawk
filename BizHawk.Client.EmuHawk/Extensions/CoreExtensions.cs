using System.Drawing;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;

using BizHawk.Emulation.Cores.Atari.Atari7800;
using BizHawk.Emulation.Cores.Nintendo.GBA;
using BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Nintendo.SNES9X;
using BizHawk.Emulation.Cores.Sega.Saturn;
using BizHawk.Emulation.Cores.Consoles.Sega.gpgx;
using BizHawk.Emulation.Cores.Sony.PSP;

using BizHawk.Client.Common;

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

			if (core is QuickNES)
			{
				return Properties.Resources.QuickNes;
			}
			else if (core is LibsnesCore)
			{
				return Properties.Resources.bsnes;
			}
			else if (core is Yabause)
			{
				return Properties.Resources.yabause;
			}
			else if (core is Atari7800)
			{
				return Properties.Resources.emu7800;
			}
			else if (core is GBA)
			{
				return Properties.Resources.meteor;
			}
			else if (core is GPGX)
			{
				return Properties.Resources.genplus;
			}
			else if (core is PSP)
			{
				return Properties.Resources.ppsspp;
			}
			else if (core is Gameboy)
			{
				return Properties.Resources.gambatte;
			}
			else if (core is Snes9x)
			{
				return Properties.Resources.snes9x;
			}
			else
			{
				return null;
			}
		}

		public static string DisplayName(this IEmulator core)
		{
			var attributes = core.Attributes();

			var str = (!attributes.Released ? "(Experimental) " : string.Empty) +
				attributes.CoreName;

			if (core is LibsnesCore)
			{
				str += " (" + ((LibsnesCore)core).CurrentProfile + ")";
			}

			return str;
		}
	}
}
