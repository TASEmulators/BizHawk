using System.IO;
using BizHawk.Common.IOExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Computers.Doom;

namespace BizHawk.Client.Common
{
	// LMP file format: https://doomwiki.org/wiki/Demo#Technical_information
	// In better detail, from archive.org: http://web.archive.org/web/20070630072856/http://demospecs.planetquake.gamespy.com/lmp/lmp.html
	[ImporterFor("Heretic", ".hereticlmp")]
	internal class HereticLmpImport : MovieImporter
	{ 
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

			// Parsing header
			byte skillLevel = (byte)sr.ReadByte();
			byte episode = (byte) sr.ReadByte();
			byte map = (byte) sr.ReadByte();
			byte player1Present = (byte) sr.ReadByte();
			byte player2Present = (byte) sr.ReadByte();
			byte player3Present = (byte) sr.ReadByte();
			byte player4Present = (byte) sr.ReadByte();

			// Setting values
			syncSettings.InputFormat = DoomControllerTypes.Heretic;
			syncSettings.SkillLevel = (DSDA.SkillLevelEnum) (skillLevel+1);
			syncSettings.InitialEpisode = episode;
			syncSettings.InitialMap = map;
			syncSettings.MultiplayerMode = DSDA.MultiplayerModeEnum.M0;
			syncSettings.MonstersRespawn = false;
			syncSettings.FastMonsters = false;
			syncSettings.NoMonsters = false;
			settings.DisplayPlayer = 0;
			syncSettings.Player1Present = player1Present is not 0;
			syncSettings.Player2Present = player2Present is not 0;
			syncSettings.Player3Present = player3Present is not 0;
			syncSettings.Player4Present = player4Present is not 0;
			syncSettings.CompatibilityMode = DSDA.CompatibilityLevelEnum.C0;

			var hereticController = new HereticController(1);
			var controller = new SimpleController(hereticController.Definition);
			controller.Definition.BuildMnemonicsCache(Result.Movie.SystemID);

			bool isFinished = false;
			while (!isFinished)
			{
				if (syncSettings.Player1Present) parsePlayer(controller, sr, 1);
				if (syncSettings.Player2Present) parsePlayer(controller, sr, 2);
				if (syncSettings.Player3Present) parsePlayer(controller, sr, 3);
				if (syncSettings.Player4Present) parsePlayer(controller, sr, 4);

				// Appending new frame
				Result.Movie.AppendFrame(controller);

				// Check termination
				if (sr.Position >= sr.Length) throw new Exception("Reached end of input movie stream without finalization byte");
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

			bool isFire = (specialValue & 0b00000001) is not 0;
			controller[$"P{playerId} Fire"] = isFire;

			bool isAction = (specialValue & 0b00000010) is not 0;
			controller[$"P{playerId} Action"] = isAction;

			byte weaponSelect = (byte) ((specialValue & 0b00011100) >> 2);
			controller.AcceptNewAxis($"P{playerId} Weapon Select", weaponSelect);

			bool altWeapon = (specialValue & 0b00100000) is not 0;
			controller[$"P{playerId} Alt Weapon"] = altWeapon;

			sbyte flylook = (sbyte) sr.ReadByte();
			controller.AcceptNewAxis($"P{playerId} Fly / Look", flylook);

			sbyte useArtifact = (sbyte) sr.ReadByte();
			controller.AcceptNewAxis($"P{playerId} Use Artifact", useArtifact);
		}
	}
}
