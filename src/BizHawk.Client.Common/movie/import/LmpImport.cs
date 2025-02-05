using System.IO;
using BizHawk.Common.IOExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Computers.Doom;

namespace BizHawk.Client.Common
{
	// LMP file format: https://doomwiki.org/wiki/Demo#Technical_information
	// In better detail, from archive.org: http://web.archive.org/web/20070630072856/http://demospecs.planetquake.gamespy.com/lmp/lmp.html
	[ImporterFor("Doom", ".lmp")]
	internal class LmpImport : MovieImporter
	{
		private enum DemoFormat
		{
			DoomUpTo12,
			DoomPost12
		}

		protected override void RunImport()
		{
			Result.Movie.HeaderEntries[HeaderKeys.Core] = CoreNames.DSDA;
			var platform = VSystemID.Raw.Doom;
			var settings = new DSDA.DoomSettings();
			var syncSettings = new DSDA.DoomSyncSettings();

			Result.Movie.SystemID = platform;

			// Getting input file stream
			var input = SourceFile.OpenRead().ReadAllBytes();
			Stream sr = new MemoryStream(input);

			// Reading signature
			var signature = sr.ReadByte();

			// Try to decide game version based on signature
			DemoFormat presumedFormat = DemoFormat.DoomUpTo12;
			DSDA.CompatibilityLevelEnum presumedCompatibilityLevel = DSDA.CompatibilityLevelEnum.C0;
			if (signature > 102) // >1.2
			{
			   presumedFormat = DemoFormat.DoomPost12;
			   if (signature < 109) presumedCompatibilityLevel = DSDA.CompatibilityLevelEnum.C1; // 1.666
			   if (signature >= 109) presumedCompatibilityLevel = DSDA.CompatibilityLevelEnum.C2; // 1.9
			   Console.WriteLine("Reading DOOM LMP demo version: {0}", signature);
			}
			else Console.WriteLine("Reading DOOM LMP demo version: <=1.12");

			// Parsing header
			byte skillLevel = (byte) signature; // For <=1.2, the first byte is already the skill level
			if (presumedFormat == DemoFormat.DoomPost12) skillLevel = (byte)sr.ReadByte();
			byte episode = (byte) sr.ReadByte();
			byte map = (byte) sr.ReadByte();
			byte multiplayerMode = (byte) sr.ReadByte();
			byte monstersRespawn = (byte) sr.ReadByte();
			byte fastMonsters = (byte) sr.ReadByte();
			byte noMonsters = (byte) sr.ReadByte();
			byte displayPlayer = (byte) sr.ReadByte();
			byte player1Present = (byte) sr.ReadByte();
			byte player2Present = (byte) sr.ReadByte();
			byte player3Present = (byte) sr.ReadByte();
			byte player4Present = (byte) sr.ReadByte();

			// Setting values
			syncSettings.SkillLevel = (DSDA.SkillLevelEnum) (skillLevel+1);
			syncSettings.InitialEpisode = episode;
			syncSettings.InitialMap = map;
			syncSettings.MultiplayerMode = (DSDA.MultiplayerModeEnum) multiplayerMode;
			syncSettings.MonstersRespawn = monstersRespawn > 0;
			syncSettings.FastMonsters = fastMonsters > 0;
			syncSettings.NoMonsters = noMonsters > 0;
			settings.DisplayPlayer = displayPlayer;
			syncSettings.Player1Present = player1Present > 0;
			syncSettings.Player2Present = player2Present > 0;
			syncSettings.Player3Present = player3Present > 0;
			syncSettings.Player4Present = player4Present > 0;
			syncSettings.CompatibilityMode = presumedCompatibilityLevel;

			var doomController1 = new DoomController(0);
			var controller = new SimpleController(doomController1.Definition);
			controller.Definition.BuildMnemonicsCache(Result.Movie.SystemID);

			bool isFinished = false;
			while (!isFinished)
			{
				if (player1Present > 0) parsePlayer(controller, sr, 0);
				if (player2Present > 0) parsePlayer(controller, sr, 1);
				if (player3Present > 0) parsePlayer(controller, sr, 2);
				if (player4Present > 0) parsePlayer(controller, sr, 3);

				// Appending new frame
				Result.Movie.AppendFrame(controller);

				// Check termination
				if (sr.ReadByte() == 0x80) isFinished = true;
				sr.Seek(-1, SeekOrigin.Current);
			}

			Result.Movie.SyncSettingsJson = ConfigService.SaveWithType(syncSettings);
		}

		private static void parsePlayer(SimpleController controller, Stream sr, int playerId)
		{
			sbyte runValue = (sbyte) sr.ReadByte();
			controller.AcceptNewAxis($"P{playerId} Run Speed", runValue);

			sbyte strafingValue = (sbyte) sr.ReadByte();
			controller.AcceptNewAxis($"P{playerId} Strafing Speed", strafingValue);

			sbyte turningValue = (sbyte) sr.ReadByte();
			controller.AcceptNewAxis($"P{playerId} Turning Speed", turningValue);

			byte specialValue = (byte) sr.ReadByte();

			bool isFire = (specialValue & 0b00000001) > 0;
			controller[$"P{playerId} Fire"] = isFire;

			bool isAction = (specialValue & 0b00000010) > 1;
			controller[$"P{playerId} Action"] = isAction;

			byte weaponSelect = (byte) ((specialValue & 0b00011100) >> 2);
			controller.AcceptNewAxis($"P{playerId} Weapon Select", weaponSelect);

			bool altWeapon = (specialValue & 0b00100000) > 5;
			controller[$"P{playerId} Alt Weapon"] = altWeapon;
		}
	}
}
