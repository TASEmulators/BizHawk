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

namespace BizHawk.Client.MultiHawk.CoreExtensions
{
	public static class CoreExtensions
	{
		//public static Bitmap Icon(this IEmulator core)
		//{
		//	var attributes = Global.Emulator.Attributes();

		//	if (!attributes.Ported)
		//	{
		//		return Properties.Resources.CorpHawkSmall;
		//	}

		//	if (Global.Emulator is QuickNES)
		//	{
		//		return Properties.Resources.QuickNes;
		//	}
		//	else if (Global.Emulator is LibsnesCore)
		//	{
		//		return Properties.Resources.bsnes;
		//	}
		//	else if (Global.Emulator is Yabause)
		//	{
		//		return Properties.Resources.yabause;
		//	}
		//	else if (Global.Emulator is Atari7800)
		//	{
		//		return Properties.Resources.emu7800;
		//	}
		//	else if (Global.Emulator is GBA)
		//	{
		//		return Properties.Resources.meteor;
		//	}
		//	else if (Global.Emulator is GPGX)
		//	{
		//		return Properties.Resources.genplus;
		//	}
		//	else if (Global.Emulator is PSP)
		//	{
		//		return Properties.Resources.ppsspp;
		//	}
		//	else if (Global.Emulator is Gameboy)
		//	{
		//		return Properties.Resources.gambatte;
		//	}
		//	else if (Global.Emulator is Snes9x)
		//	{
		//		return Properties.Resources.snes9x;
		//	}
		//	else
		//	{
		//		return null;
		//	}
		//}

		public static string DisplayName(this IEmulator core)
		{
			var attributes = Global.Emulator.Attributes();

			var str = (!attributes.Released ? "(Experimental) " : string.Empty) +
				attributes.CoreName;

			if (Global.Emulator is LibsnesCore)
			{
				str += " (" + ((LibsnesCore)Global.Emulator).CurrentProfile + ")";
			}

			return str;
		}
	}
}
