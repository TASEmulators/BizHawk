using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Atari.Jaguar;
using BizHawk.Emulation.Cores.Consoles.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Consoles.Sega.gpgx;
using BizHawk.Emulation.Cores.Consoles.Sega.PicoDrive;
using BizHawk.Emulation.Cores.Nintendo.BSNES;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Nintendo.GBHawkLink;
using BizHawk.Emulation.Cores.Nintendo.GBHawkLink3x;
using BizHawk.Emulation.Cores.Nintendo.GBHawkLink4x;
using BizHawk.Emulation.Cores.Nintendo.SNES;

namespace BizHawk.Client.EmuHawk
{
	public partial class RetroAchievements
	{
		private RAInterface.ConsoleID SystemIdToConsoleId()
		{
			return Emu.SystemId switch
			{
				VSystemID.Raw.A26 => RAInterface.ConsoleID.Atari2600,
				VSystemID.Raw.A78 => RAInterface.ConsoleID.Atari7800,
				VSystemID.Raw.Amiga => RAInterface.ConsoleID.Amiga,
				VSystemID.Raw.AmstradCPC => RAInterface.ConsoleID.AmstradCPC,
				VSystemID.Raw.AppleII => RAInterface.ConsoleID.AppleII,
				VSystemID.Raw.C64 => RAInterface.ConsoleID.C64,
				VSystemID.Raw.ChannelF => RAInterface.ConsoleID.FairchildChannelF,
				VSystemID.Raw.Coleco => RAInterface.ConsoleID.Colecovision,
				VSystemID.Raw.DEBUG => RAInterface.ConsoleID.UnknownConsoleID,
				VSystemID.Raw.Dreamcast => RAInterface.ConsoleID.Dreamcast,
				VSystemID.Raw.GameCube => RAInterface.ConsoleID.GameCube,
				VSystemID.Raw.GB when Emu is IGameboyCommon gb => gb.IsCGBMode() ? RAInterface.ConsoleID.GBC : RAInterface.ConsoleID.GB,
				VSystemID.Raw.GBA => RAInterface.ConsoleID.GBA,
				VSystemID.Raw.GBC => RAInterface.ConsoleID.GBC, // Not actually used
				VSystemID.Raw.GBL => Emu switch // actually can be a mix of GB and GBC
				{
					// there's probably a better way for all this
					GambatteLink gb => gb.IsCGBMode(0) ? RAInterface.ConsoleID.GBC : RAInterface.ConsoleID.GB,
					// WHY ARE THESE PUBLIC???
					GBHawkLink gb => gb.L.IsCGBMode() ? RAInterface.ConsoleID.GBC : RAInterface.ConsoleID.GB,
					GBHawkLink3x gb => gb.L.IsCGBMode() ? RAInterface.ConsoleID.GBC : RAInterface.ConsoleID.GB,
					GBHawkLink4x gb => gb.A.IsCGBMode() ? RAInterface.ConsoleID.GBC : RAInterface.ConsoleID.GB,
					_ => RAInterface.ConsoleID.UnknownConsoleID,
				},
				VSystemID.Raw.GEN when Emu is GPGX gpgx => gpgx.IsMegaCD ? RAInterface.ConsoleID.SegaCD : RAInterface.ConsoleID.MegaDrive,
				VSystemID.Raw.GEN when Emu is PicoDrive pico => pico.Is32XActive ? RAInterface.ConsoleID.Sega32X : RAInterface.ConsoleID.MegaDrive,
				VSystemID.Raw.GG => RAInterface.ConsoleID.GameGear,
				VSystemID.Raw.GGL => RAInterface.ConsoleID.GameGear, // ???
				VSystemID.Raw.INTV => RAInterface.ConsoleID.Intellivision,
				VSystemID.Raw.Jaguar when Emu is VirtualJaguar jaguar => jaguar.IsJaguarCD ? RAInterface.ConsoleID.JaguarCD : RAInterface.ConsoleID.Jaguar,
				VSystemID.Raw.Libretro => RAInterface.ConsoleID.UnknownConsoleID,
				VSystemID.Raw.Lynx => RAInterface.ConsoleID.Lynx,
				VSystemID.Raw.MAME => RAInterface.ConsoleID.Arcade,
				VSystemID.Raw.MSX => RAInterface.ConsoleID.MSX,
				VSystemID.Raw.N64 => RAInterface.ConsoleID.N64,
				VSystemID.Raw.NDS => RAInterface.ConsoleID.DS,
				VSystemID.Raw.NeoGeoCD => RAInterface.ConsoleID.NeoGeoCD,
				VSystemID.Raw.NES => RAInterface.ConsoleID.NES,
				VSystemID.Raw.NGP => RAInterface.ConsoleID.NeoGeoPocket,
				VSystemID.Raw.NULL => RAInterface.ConsoleID.UnknownConsoleID,
				VSystemID.Raw.O2 => RAInterface.ConsoleID.MagnavoxOdyssey,
				VSystemID.Raw.Panasonic3DO => RAInterface.ConsoleID.ThreeDO,
				VSystemID.Raw.PCE => RAInterface.ConsoleID.PCEngine,
				VSystemID.Raw.PCECD => RAInterface.ConsoleID.PCEngineCD,
				VSystemID.Raw.PCFX => RAInterface.ConsoleID.PCFX,
				VSystemID.Raw.PhillipsCDi => RAInterface.ConsoleID.CDi,
				VSystemID.Raw.Playdia => RAInterface.ConsoleID.UnknownConsoleID,
				VSystemID.Raw.PS2 => RAInterface.ConsoleID.PlayStation2,
				VSystemID.Raw.PSP => RAInterface.ConsoleID.PSP,
				VSystemID.Raw.PSX => RAInterface.ConsoleID.PlayStation,
				VSystemID.Raw.SAT => RAInterface.ConsoleID.Saturn,
				VSystemID.Raw.Sega32X => RAInterface.ConsoleID.Sega32X, // not actually used
				VSystemID.Raw.SG => RAInterface.ConsoleID.SG1000,
				VSystemID.Raw.SGB => RAInterface.ConsoleID.GB,
				VSystemID.Raw.SGX => RAInterface.ConsoleID.PCEngine, // ???
				VSystemID.Raw.SGXCD => RAInterface.ConsoleID.PCEngineCD, // ???
				VSystemID.Raw.SMS => RAInterface.ConsoleID.MasterSystem,
				VSystemID.Raw.SNES => Emu switch
				{
					LibsnesCore libsnes => libsnes.IsSGB ? RAInterface.ConsoleID.GB : RAInterface.ConsoleID.SNES,
					BsnesCore bsnes => bsnes.IsSGB ? RAInterface.ConsoleID.GB : RAInterface.ConsoleID.SNES,
					_ => RAInterface.ConsoleID.SNES,
				},
				VSystemID.Raw.TI83 => RAInterface.ConsoleID.UnknownConsoleID,
				VSystemID.Raw.TIC80 => RAInterface.ConsoleID.Tic80,
				VSystemID.Raw.UZE => RAInterface.ConsoleID.UnknownConsoleID,
				VSystemID.Raw.VB => RAInterface.ConsoleID.VirtualBoy,
				VSystemID.Raw.VEC => RAInterface.ConsoleID.Vectrex,
				VSystemID.Raw.Wii => RAInterface.ConsoleID.WII,
				VSystemID.Raw.WSWAN => RAInterface.ConsoleID.WonderSwan,
				VSystemID.Raw.ZXSpectrum => RAInterface.ConsoleID.ZXSpectrum,
				_ => RAInterface.ConsoleID.UnknownConsoleID,
			};
		}
	}
}
