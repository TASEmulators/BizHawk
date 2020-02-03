using System.Drawing;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;

using BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Nintendo.SNES9X;
using BizHawk.Emulation.Cores.Consoles.Sega.gpgx;
using BizHawk.Emulation.Cores.Sony.PSP;
using BizHawk.Emulation.Cores.Arcades.MAME;
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

			if (core is MGBAHawk)
			{
				return Properties.Resources.mGba;
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

		public static SystemInfo System(this IEmulator emulator)
		{
			switch (emulator.SystemId)
			{ 
				default:
				case "NULL":
					return SystemInfo.Null;
				case "NES":
					return SystemInfo.Nes;
				case "INTV":
					return SystemInfo.Intellivision;
				case "SG":
					return SystemInfo.SG;
				case "SMS":
					if (emulator is SMS gg && gg.IsGameGear)
					{
						return SystemInfo.GG;
					}

					if (emulator is SMS sg && sg.IsSG1000)
					{
						return SystemInfo.SG;
					}

					return SystemInfo.SMS;
				case "PCECD":
					return SystemInfo.PCECD;
				case "PCE":
					return SystemInfo.PCE;
				case "SGX":
					return SystemInfo.SGX;
				case "GEN":
					return SystemInfo.Genesis;
				case "TI83":
					return SystemInfo.TI83;
				case "SNES":
					return SystemInfo.SNES;
				case "GB":
					/*
					if ((Emulator as IGameboyCommon).IsCGBMode())
					{
						return SystemInfo.GBC;
					}
					*/
					return SystemInfo.GB;
				case "A26":
					return SystemInfo.Atari2600;
				case "A78":
					return SystemInfo.Atari7800;
				case "C64":
					return SystemInfo.C64;
				case "Coleco":
					return SystemInfo.Coleco;
				case "GBA":
					return SystemInfo.GBA;
				case "N64":
					return SystemInfo.N64;
				case "SAT":
					return SystemInfo.Saturn;
				case "DGB":
					return SystemInfo.DualGB;
				case "GB3x":
					return SystemInfo.GB3x;
				case "GB4x":
					return SystemInfo.GB4x;
				case "WSWAN":
					return SystemInfo.WonderSwan;
				case "Lynx":
					return SystemInfo.Lynx;
				case "PSX":
					return SystemInfo.PSX;
				case "AppleII":
					return SystemInfo.AppleII;
				case "Libretro":
					return SystemInfo.Libretro;
				case "VB":
					return SystemInfo.VirtualBoy;
				case "VEC":
					return SystemInfo.Vectrex;
				case "NGP":
					return SystemInfo.NeoGeoPocket;
				case "ZXSpectrum":
					return SystemInfo.ZxSpectrum;
				case "AmstradCPC":
					return SystemInfo.AmstradCpc;
				case "ChannelF":
					return SystemInfo.ChannelF;
				case "O2":
					return SystemInfo.O2;
				case "MAME":
					return SystemInfo.Mame;
			}
		}
	}
}
