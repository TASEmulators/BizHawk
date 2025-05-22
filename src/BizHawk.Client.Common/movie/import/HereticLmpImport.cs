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
			var input = SourceFile.OpenRead().ReadAllBytes();
			var i = 0;
			Result.Movie.HeaderEntries[HeaderKeys.Core] = CoreNames.DSDA;
			Result.Movie.SystemID = VSystemID.Raw.Doom;
			DSDA.DoomSyncSettings syncSettings = new()
			{
				InputFormat = DSDA.ControllerTypes.Heretic,
				MultiplayerMode = DSDA.MultiplayerMode.Single_Coop,
				MonstersRespawn = false,
				FastMonsters = false,
				NoMonsters = false,
				CompatibilityLevel = DSDA.CompatibilityLevel.Doom_12,
				SkillLevel = (DSDA.SkillLevel) (1 + input[i++]),
				InitialEpisode = input[i++],
				InitialMap = input[i++],
				Player1Present = input[i++] is not 0,
				Player2Present = input[i++] is not 0,
				Player3Present = input[i++] is not 0,
				Player4Present = input[i++] is not 0,
				TurningResolution = DSDA.TurningResolution.Shorttics,
				RenderWipescreen = false,
			};
			Result.Movie.SyncSettingsJson = ConfigService.SaveWithType(syncSettings);

			var controller = new SimpleController(DSDA.CreateControllerDefinition(syncSettings));
			controller.Definition.BuildMnemonicsCache(Result.Movie.SystemID);
			void ParsePlayer(string playerPfx)
			{
				controller.AcceptNewAxis(playerPfx + "Run Speed", unchecked((sbyte) input[i++]));
				controller.AcceptNewAxis(playerPfx + "Strafing Speed", unchecked((sbyte) input[i++]));
				controller.AcceptNewAxis(playerPfx + "Turning Speed", unchecked((sbyte) input[i++]));
				var specialValue = input[i++];
				controller[playerPfx + "Fire"] = (specialValue & 0b00000001) is not 0;
				controller[playerPfx + "Use"] = (specialValue & 0b00000010) is not 0;
				bool changeWeapon = (specialValue & 0b00000100) is not 0;
				int weapon = changeWeapon ? (((specialValue & 0b00111000) >> 3) + 1) : 0;
				controller.AcceptNewAxis(playerPfx + "Weapon Select", weapon);
				controller.AcceptNewAxis(playerPfx + "Fly / Look", unchecked((sbyte) input[i++]));
				controller.AcceptNewAxis(playerPfx + "Use Artifact", unchecked((sbyte) input[i++]));
			}
			do
			{
				if (syncSettings.Player1Present) ParsePlayer("P1 ");
				if (syncSettings.Player2Present) ParsePlayer("P2 ");
				if (syncSettings.Player3Present) ParsePlayer("P3 ");
				if (syncSettings.Player4Present) ParsePlayer("P4 ");
				Result.Movie.AppendFrame(controller);
				if (i == input.Length) throw new Exception("Reached end of input movie stream without finalization byte");
			}
			while (input[i] is not 0x80);
		}
	}
}
