using System.Drawing;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;

using BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Nintendo.SNES9X;
using BizHawk.Emulation.Cores.Consoles.Sega.gpgx;
using BizHawk.Emulation.Cores.Sony.PSP;
using BizHawk.Emulation.Cores.Arcades.MAME;

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
			
			if (core is LibsnesCore)
			{
				return Properties.Resources.bsnes;
			}
			
			if (core is GPGX)
			{
				return Properties.Resources.genplus;
			}
			
			if (core is PSP)
			{
				return Properties.Resources.ppsspp;
			}
			
			if (core is Gameboy)
			{
				return Properties.Resources.gambatte;
			}
			
			if (core is Snes9x)
			{
				return Properties.Resources.snes9x;
			}
			
			if (core is MAME)
			{
				return Properties.Resources.mame;
			}

			return null;
		}

		public static string DisplayName(this IEmulator core)
		{
			var attributes = core.Attributes();

			var str = (!attributes.Released ? "(Experimental) " : "") +
				attributes.CoreName;

			return str;
		}
	}
}
