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
			var input = SourceFile.OpenRead().ReadAllBytes();
			var i = 0;

			Result.Movie.HeaderEntries[HeaderKeys.Core] = CoreNames.DSDA;
			Result.Movie.SystemID = VSystemID.Raw.Doom;

			DSDA.DoomSyncSettings syncSettings = new()
			{
				InputFormat = DSDA.ControllerType.Hexen,
				MultiplayerMode = DSDA.MultiplayerMode.Single_Coop,
				MonstersRespawn = false,
				FastMonsters = false,
				NoMonsters = false,
				CompatibilityLevel = DSDA.CompatibilityLevel.Doom_12,
				SkillLevel = (DSDA.SkillLevel) (1 + input[i++]),
				InitialEpisode = input[i++],
				InitialMap = input[i++],
				Player1Present = input[i++] is not 0,
				Player1Class = (DSDA.HexenClass) input[i++],
				Player2Present = input[i++] is not 0,
				Player2Class = (DSDA.HexenClass) input[i++],
				Player3Present = input[i++] is not 0,
				Player3Class = (DSDA.HexenClass) input[i++],
				Player4Present = input[i++] is not 0,
				Player4Class = (DSDA.HexenClass) input[i++],
				TurningResolution = DSDA.TurningResolution.Shorttics,
				RenderWipescreen = false,
			};

			_ = input[i++]; // player 5 isPresent
			_ = input[i++]; // player 5 class
			_ = input[i++]; // player 6 isPresent
			_ = input[i++]; // player 6 class
			_ = input[i++]; // player 7 isPresent
			_ = input[i++]; // player 7 class
			_ = input[i++]; // player 8 isPresent
			_ = input[i++]; // player 8 class

			Result.Movie.SyncSettingsJson = ConfigService.SaveWithType(syncSettings);

			var controller = new SimpleController(DSDA.CreateControllerDefinition(syncSettings));
			controller.Definition.BuildMnemonicsCache(Result.Movie.SystemID);

			void ParsePlayer(string port)
			{
				controller.AcceptNewAxis(port + "Run Speed",    unchecked((sbyte) input[i++]));
				controller.AcceptNewAxis(port + "Strafe Speed", unchecked((sbyte) input[i++]));
				controller.AcceptNewAxis(port + "Turn Speed",   unchecked((sbyte) input[i++]));

				var buttons = (LibDSDA.Buttons)input[i++];
				controller[port + "Fire"] = (buttons & LibDSDA.Buttons.Fire) is not 0;
				controller[port + "Use" ] = (buttons & LibDSDA.Buttons.Use ) is not 0;
				var changeWeapon = (buttons & LibDSDA.Buttons.ChangeWeapon) is not 0;

				var weapon = changeWeapon
					? (((int)(buttons & LibDSDA.Buttons.WeaponMask) >> 3) + 1)
					: 0;
				controller.AcceptNewAxis(port + "Weapon Select", weapon);

				int flylook = unchecked((sbyte) input[i++]);
				int look = flylook & 15; if (look > 8) look -= 16;
				int fly  = flylook >> 4; if (fly  > 8) fly  -= 16;
				controller.AcceptNewAxis(port + "Look", look);
				controller.AcceptNewAxis(port + "Fly", fly);

				var useArtifact = input[i++];
				controller.AcceptNewAxis(port + "Use Artifact",
					useArtifact & (int)LibDSDA.Buttons.ArtifactMask);

				controller[port + "End Player"] = (useArtifact & (int)LibDSDA.Buttons.EndPlayer) is not 0;
				controller[port + "Jump"      ] = (useArtifact & (int)LibDSDA.Buttons.Jump     ) is not 0;
			}

			do
			{
				if (syncSettings.Player1Present) ParsePlayer("P1 ");
				if (syncSettings.Player2Present) ParsePlayer("P2 ");
				if (syncSettings.Player3Present) ParsePlayer("P3 ");
				if (syncSettings.Player4Present) ParsePlayer("P4 ");

				Result.Movie.AppendFrame(controller);

				if (i == input.Length)
					throw new Exception("Reached end of input movie stream without finalization byte");
			}
			while (input[i] is not 0x80);
		}
	}
}
