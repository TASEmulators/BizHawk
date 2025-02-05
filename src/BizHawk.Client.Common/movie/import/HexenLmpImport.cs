using System.IO;
using BizHawk.Common.IOExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Computers.Doom;

namespace BizHawk.Client.Common
{
	// LMP file format: https://doomwiki.org/wiki/Demo#Technical_information
	// In better detail, from archive.org: http://web.archive.org/web/20070630072856/http://demospecs.planetquake.gamespy.com/lmp/lmp.html
	[ImporterFor("Hexen", ".hexenlmp")]
	internal class HexenLmpImport : MovieImporter
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
			byte player1Class = (byte) sr.ReadByte();
			byte player2Present = (byte) sr.ReadByte();
			byte player2Class = (byte) sr.ReadByte();
			byte player3Present = (byte) sr.ReadByte();
			byte player3Class = (byte) sr.ReadByte();
			byte player4Present = (byte) sr.ReadByte();
			byte player4Class = (byte) sr.ReadByte();

			// Players [5-8] ignored
			byte player5Present = (byte) sr.ReadByte();
			byte player5Class = (byte) sr.ReadByte();
			byte player6Present = (byte) sr.ReadByte();
			byte player6Class = (byte) sr.ReadByte();
			byte player7Present = (byte) sr.ReadByte();
			byte player7Class = (byte) sr.ReadByte();
			byte playerPresent = (byte) sr.ReadByte();
			byte player8Class = (byte) sr.ReadByte();

			// Setting values
			syncSettings.SkillLevel = (DSDA.SkillLevelEnum) (skillLevel+1);
			syncSettings.InitialEpisode = episode;
			syncSettings.InitialMap = map;
			syncSettings.MultiplayerMode = DSDA.MultiplayerModeEnum.M0;
			syncSettings.MonstersRespawn = false;
			syncSettings.FastMonsters = false;
			syncSettings.NoMonsters = false;
			settings.DisplayPlayer = 0;
			syncSettings.Player1Present = player1Present > 0;
			syncSettings.Player1Class = (DSDA.HexenClassEnum) player1Class;
			syncSettings.Player2Present = player2Present > 0;
			syncSettings.Player2Class = (DSDA.HexenClassEnum) player2Class;
			syncSettings.Player3Present = player3Present > 0;
			syncSettings.Player3Class = (DSDA.HexenClassEnum) player3Class;
			syncSettings.Player4Present = player4Present > 0;
			syncSettings.Player4Class = (DSDA.HexenClassEnum) player4Class;
			syncSettings.CompatibilityMode = DSDA.CompatibilityLevelEnum.C0;

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

			bool isFire = (specialValue & 0b00000001) > 0;
			controller[$"P{playerId} Fire"] = isFire;

			bool isAction = (specialValue & 0b00000010) > 1;
			controller[$"P{playerId} Action"] = isAction;

			byte weaponSelect = (byte) ((specialValue & 0b00011100) >> 2);
			controller.AcceptNewAxis($"P{playerId} Weapon Select", weaponSelect);

			bool altWeapon = (specialValue & 0b00100000) > 5;
			controller[$"P{playerId} Alt Weapon"] = altWeapon;

			sbyte flylook = (sbyte) sr.ReadByte();
			controller.AcceptNewAxis($"P{playerId} Fly / Look", flylook);

			sbyte useArtifact = (sbyte) sr.ReadByte();
			controller.AcceptNewAxis($"P{playerId} Use Artifact", useArtifact & 0b00111111);

			bool isJump = (useArtifact & 0b10000000) > 1;
			controller[$"P{playerId} Jump"] = isJump;

			bool isEndPlayer = (useArtifact & 0b01000000) > 1;
			controller[$"P{playerId} End Player"] = isEndPlayer;
		}
	}
}
