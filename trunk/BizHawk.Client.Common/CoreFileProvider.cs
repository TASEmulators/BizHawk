using System;
using System.IO;

namespace BizHawk.Client.Common
{
	public class CoreFileProvider : ICoreFileProvider
	{
		public string SubfileDirectory;
		public FirmwareManager FirmwareManager;

		public Stream OpenFirmware(string sysId, string key)
		{
			var fn = PathFirmware(sysId, key);
			return new FileStream(fn, FileMode.Open, FileAccess.Read, FileShare.Read);
		}

		public string PathFirmware(string sysId, string key)
		{
			return FirmwareManager.Request(sysId, key);
		}

		public string PathSubfile(string fname)
		{
			return Path.Combine(Path.GetDirectoryName(SubfileDirectory) ?? String.Empty, fname);
		}

		public static void SyncCoreCommInputSignals(CoreComm target = null)
		{
			if (target == null)
			{
				target = Global.CoreComm;
			}

			var cfp = new CoreFileProvider();
			target.CoreFileProvider = cfp;
			cfp.FirmwareManager = Global.FirmwareManager;

			target.NES_BackdropColor = Global.Config.NESBackgroundColor;
			target.NES_UnlimitedSprites = Global.Config.NESAllowMoreThanEightSprites;
			target.NES_ShowBG = Global.Config.NESDispBackground;
			target.NES_ShowOBJ = Global.Config.NESDispSprites;
			target.PCE_ShowBG1 = Global.Config.PCEDispBG1;
			target.PCE_ShowOBJ1 = Global.Config.PCEDispOBJ1;
			target.PCE_ShowBG2 = Global.Config.PCEDispBG2;
			target.PCE_ShowOBJ2 = Global.Config.PCEDispOBJ2;
			target.SMS_ShowBG = Global.Config.SMSDispBG;
			target.SMS_ShowOBJ = Global.Config.SMSDispOBJ;

			target.PSX_FirmwaresPath = PathManager.MakeAbsolutePath(Global.Config.PathEntries.FirmwaresPath, null); // PathManager.MakeAbsolutePath(Global.Config.PathPSXFirmwares, "PSX");

			target.C64_FirmwaresPath = PathManager.MakeAbsolutePath(Global.Config.PathEntries.FirmwaresPath, null); // PathManager.MakeAbsolutePath(Global.Config.PathC64Firmwares, "C64");

			target.SNES_FirmwaresPath = PathManager.MakeAbsolutePath(Global.Config.PathEntries.FirmwaresPath, null); // PathManager.MakeAbsolutePath(Global.Config.PathSNESFirmwares, "SNES");
			target.SNES_ShowBG1_0 = Global.Config.SNES_ShowBG1_0;
			target.SNES_ShowBG1_1 = Global.Config.SNES_ShowBG1_1;
			target.SNES_ShowBG2_0 = Global.Config.SNES_ShowBG2_0;
			target.SNES_ShowBG2_1 = Global.Config.SNES_ShowBG2_1;
			target.SNES_ShowBG3_0 = Global.Config.SNES_ShowBG3_0;
			target.SNES_ShowBG3_1 = Global.Config.SNES_ShowBG3_1;
			target.SNES_ShowBG4_0 = Global.Config.SNES_ShowBG4_0;
			target.SNES_ShowBG4_1 = Global.Config.SNES_ShowBG4_1;
			target.SNES_ShowOBJ_0 = Global.Config.SNES_ShowOBJ1;
			target.SNES_ShowOBJ_1 = Global.Config.SNES_ShowOBJ2;
			target.SNES_ShowOBJ_2 = Global.Config.SNES_ShowOBJ3;
			target.SNES_ShowOBJ_3 = Global.Config.SNES_ShowOBJ4;

			target.SNES_Profile = Global.Config.SNESProfile;
			target.SNES_UseRingBuffer = Global.Config.SNESUseRingBuffer;
			target.SNES_AlwaysDoubleSize = Global.Config.SNESAlwaysDoubleSize;

			target.GG_HighlightActiveDisplayRegion = Global.Config.GGHighlightActiveDisplayRegion;
			target.GG_ShowClippedRegions = Global.Config.GGShowClippedRegions;

			target.Atari2600_ShowBG = Global.Config.Atari2600_ShowBG;
			target.Atari2600_ShowPlayer1 = Global.Config.Atari2600_ShowPlayer1;
			target.Atari2600_ShowPlayer2 = Global.Config.Atari2600_ShowPlayer2;
			target.Atari2600_ShowMissle1 = Global.Config.Atari2600_ShowMissle1;
			target.Atari2600_ShowMissle2 = Global.Config.Atari2600_ShowMissle2;
			target.Atari2600_ShowBall = Global.Config.Atari2600_ShowBall;
			target.Atari2600_ShowPF = Global.Config.Atari2600_ShowPlayfield;
		}
	}
}