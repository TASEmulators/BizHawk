using System.IO;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Computers.Doom;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Sega.MasterSystem;
using SharpGen.Runtime;

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
			using var sr = SourceFile.OpenText();

			// Checking it's not empty
			if (sr.EndOfStream)
			{
				Result.Errors.Add("This is an empty file.");
				return;
			}

			// Reading signature
			var signature = sr.Read();

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
			if (presumedFormat == DemoFormat.DoomPost12) skillLevel = (byte)sr.Read();
			byte episode = (byte) sr.Read();
			byte map = (byte) sr.Read();
			byte multiplayerMode = (byte) sr.Read();
			byte monstersRespawn = (byte) sr.Read();
			byte fastMonsters = (byte) sr.Read();
			byte noMonsters = (byte) sr.Read();
			byte displayPlayer = (byte) sr.Read();
			byte player1Present = (byte) sr.Read();
			byte player2Present = (byte) sr.Read();
			byte player3Present = (byte) sr.Read();
			byte player4Present = (byte) sr.Read();

			// Setting values
			syncSettings.SkillLevel = (DSDA.SkillLevelEnum) skillLevel;
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

			while (!sr.EndOfStream && (byte) sr.Read() != 0x80)
			{
				if (player1Present > 0) parsePlayer(sr);
				if (player2Present > 0) parsePlayer(sr);
				if (player3Present > 0) parsePlayer(sr);
				if (player4Present > 0) parsePlayer(sr);

				// Where do I get this controller object from?
				// Result.Movie.AppendFrame(controller);
			}

			Result.Movie.SyncSettingsJson = ConfigService.SaveWithType(syncSettings);
		}

		private void parsePlayer(StreamReader sr)
		{
			sbyte runValue = (sbyte) sr.Read();
			sbyte strafingValue = (sbyte) sr.Read();
			sbyte turningValue = (sbyte) sr.Read();
			byte specialValue = (byte) sr.Read();

			bool isFire = (specialValue & 0b00000001) > 0;
			bool isAction = (specialValue & 0b00000010) > 1;
			byte weaponSelect = (byte) ((specialValue & 0b00011100) >> 2);
			bool altWeapon = (specialValue & 0b00100000) > 5;
		}
	}
}
