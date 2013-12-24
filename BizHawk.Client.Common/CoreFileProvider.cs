using System;
using System.IO;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class CoreFileProvider : ICoreFileProvider
	{
		public string SubfileDirectory;
		public FirmwareManager FirmwareManager;

		Action<string> ShowWarning;

		public CoreFileProvider(Action<string> ShowWarning)
		{
			this.ShowWarning = ShowWarning;
		}

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

		#region EmuLoadHelper api
		void FirmwareWarn(string sysID, string firmwareID, bool required, string msg = null)
		{
			if (required)
			{
				string fullmsg = string.Format(
					"Couldn't find required firmware \"{0}:{1}\".  This is fatal{2}", sysID, firmwareID, msg != null ? ": " + msg : ".");
				throw new Exception(fullmsg);
			}
			else
			{
				if (msg != null)
				{
					string fullmsg = string.Format(
						"Couldn't find firmware \"{0}:{1}\".  Will attempt to continue: {2}", sysID, firmwareID, msg);
					ShowWarning(msg);
				}
			}
		}


		public string GetFirmwarePath(string sysID, string firmwareID, bool required, string msg = null)
		{
			string path = FirmwareManager.Request(sysID, firmwareID);
			if (path != null && !File.Exists(path))
				path = null;

			if (path == null)
				FirmwareWarn(sysID, firmwareID, required, msg);
			return path;
		}

		public byte[] GetFirmware(string sysID, string firmwareID, bool required, string msg = null)
		{
			byte[] ret = null;
			string path = GetFirmwarePath(sysID, firmwareID, required, msg);
			if (path != null && File.Exists(path))
			{
				try
				{
					ret = File.ReadAllBytes(path);
				}
				catch (IOException) { }
			}

			if (ret == null && path != null)
				FirmwareWarn(sysID, firmwareID, required, msg);
			return ret;
		}

		#endregion

		public static void SyncCoreCommInputSignals(CoreComm target = null)
		{
			if (target == null)
			{
				target = Global.CoreComm;
			}

			var cfp = new CoreFileProvider(target.ShowMessage);
			target.CoreFileProvider = cfp;
			cfp.FirmwareManager = Global.FirmwareManager;

			//target.NES_BackdropColor = Global.Config.NESBackgroundColor;
			//target.NES_UnlimitedSprites = Global.Config.NESAllowMoreThanEightSprites;
			//target.NES_ShowBG = Global.Config.NESDispBackground;
			//target.NES_ShowOBJ = Global.Config.NESDispSprites;
			//target.PCE_ShowBG1 = Global.Config.PCEDispBG1;
			//target.PCE_ShowOBJ1 = Global.Config.PCEDispOBJ1;
			//target.PCE_ShowBG2 = Global.Config.PCEDispBG2;
			//target.PCE_ShowOBJ2 = Global.Config.PCEDispOBJ2;
			//target.SMS_ShowBG = Global.Config.SMSDispBG;
			//target.SMS_ShowOBJ = Global.Config.SMSDispOBJ;

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

			//target.GG_HighlightActiveDisplayRegion = Global.Config.GGHighlightActiveDisplayRegion;
			//target.GG_ShowClippedRegions = Global.Config.GGShowClippedRegions;

			//target.Atari2600_ShowBG = Global.Config.Atari2600_ShowBG;
			//target.Atari2600_ShowPlayer1 = Global.Config.Atari2600_ShowPlayer1;
			//target.Atari2600_ShowPlayer2 = Global.Config.Atari2600_ShowPlayer2;
			//target.Atari2600_ShowMissle1 = Global.Config.Atari2600_ShowMissle1;
			//target.Atari2600_ShowMissle2 = Global.Config.Atari2600_ShowMissle2;
			//target.Atari2600_ShowBall = Global.Config.Atari2600_ShowBall;
			//target.Atari2600_ShowPF = Global.Config.Atari2600_ShowPlayfield;
		}
	}
}