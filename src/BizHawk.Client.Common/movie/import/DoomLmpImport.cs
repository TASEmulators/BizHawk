using BizHawk.Common.IOExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Computers.Doom;

namespace BizHawk.Client.Common
{
	// LMP file format: https://doomwiki.org/wiki/Demo#Technical_information
	// In better detail, from archive.org: http://web.archive.org/web/20070630072856/http://demospecs.planetquake.gamespy.com/lmp/lmp.html
	// https://www.doomworld.com/forum/topic/120007-specifications-for-source-port-demo-formats
	[ImporterFor("Doom", ".lmp")]
	internal class DoomLmpImport : MovieImporter
	{
		private enum DemoVersion : int
		{
			Skill_1      = 0,
			Skill_5      = 4,
			Doom_1_4     = 104, // first Doom to write version to demo
			Doom_1_666   = 106,
			Doom_1_9     = 109, // Doom/Doom2/Ultimate/Final
			TASDoom      = 110,
			DoomClassic  = 111, // first longtics support
			Boom_2_00    = 200,
			Boom_2_01    = 201,
			Boom_2_02    = 202,
			MBF          = 203, // LxDoom/MBF
			PrBoom_2_1_0 = 210,
			// this matching looks weird but it's how DSDA-Doom parses them
			PrBoom_2_2_x = 211,
			PrBoom_2_3_x = 212,
			PrBoom_2_4_0 = 213,
			PrBoomPlus   = 214,
			MBF21        = 221,
		}

		protected override void RunImport()
		{
			var input = SourceFile.OpenRead().ReadAllBytes();
			var i = 0;

			// version dependent settings
			var compLevel         = DSDA.CompatibilityLevel.MBF21;
			var turningResolution = DSDA.TurningResolution.Shorttics;
			var skill             = DSDA.SkillLevel.UV;
			var episode           = 1;
			var map               = 0;
			// v1.2- demos didn't store these (nor DisplayPlayer), they have to be explicitly set
			var multiplayerMode   = DSDA.MultiplayerMode.Single_Coop;
			var monstersRespawn   = false;
			var fastMonsters      = false;
			var noMonsters        = false;
			//var rngSeed           = 1993U;

			Result.Movie.HeaderEntries[HeaderKeys.Core] = CoreNames.DSDA;
			Result.Movie.SystemID = VSystemID.Raw.Doom;

			// Try to decide game version
			var version = (DemoVersion)input[i++];

			// Handling of unrecognized demo formats
			// Versions up to 1.2 use a 7-byte header - first byte is a skill level.
			// Versions after 1.2 use a 13-byte header - first byte is a demoversion.
			// BOOM's demoversion starts from 200
			if (!((version >= DemoVersion.Skill_1   && version <= DemoVersion.Skill_5    ) ||
			      (version >= DemoVersion.Doom_1_4  && version <= DemoVersion.DoomClassic) ||
			      (version >= DemoVersion.Boom_2_00 && version <= DemoVersion.PrBoomPlus ) ||
			      (version == DemoVersion.MBF21)))
			{
				Result.Errors.Add($"Unknown demo format: {version}");
				return;
			}

			if (version < DemoVersion.Doom_1_4)
			{
				// there is no version, the first byte is the skill level
				skill     = (DSDA.SkillLevel)version;
				episode   = input[i++];
				map       = input[i++];
				compLevel = DSDA.CompatibilityLevel.Doom_12;
				Console.WriteLine("Reading DOOM LMP demo version: 1.2-");
			}
			else if (version < DemoVersion.Boom_2_00)
			{
				if (version == DemoVersion.TASDoom)
				{
					compLevel = DSDA.CompatibilityLevel.TasDoom;
				}
				else if (version >= DemoVersion.DoomClassic)
				{
					turningResolution = DSDA.TurningResolution.Longtics;
				}

				skill           = (DSDA.SkillLevel) (input[i++] + 1);
				episode         = input[i++];
				map             = input[i++];
				multiplayerMode = (DSDA.MultiplayerMode) input[i++];
				monstersRespawn = input[i++] is not 0;
				fastMonsters    = input[i++] is not 0;
				noMonsters      = input[i++] is not 0;
				i++; // DisplayPlayer is a non-sync setting so importers can't set it

				// DSDA-Doom assumes 1.666 compat for sig < 107 but this should be fine too
				compLevel = version < DemoVersion.Doom_1_9
					? DSDA.CompatibilityLevel.Doom_1666
					: DSDA.CompatibilityLevel.Doom2_19;
				Console.WriteLine("Reading DOOM LMP demo version: {0}", version);
			}
			else // Boom territory
			{
				Result.Errors.Add($"Found BOOM demo format: v{(int)version}. Importing it is currently not supported.");
				return;

				/*
				i++; // skip to signature's second byte
				var portID = input[i++];
				i += 4; // skip the rest of the signature
				switch (version)
				{
					case DemoVersion.Boom_2_00:
					case DemoVersion.Boom_2_01:
						if (input[i++] == 1)
						{
							compLevel = DSDA.CompatibilityLevel.Boom_Compatibility;
						}
						else
						{
							compLevel = DSDA.CompatibilityLevel.Boom_201;
						}
						break;
					case DemoVersion.Boom_2_02:
						if (input[i++] == 1)
						{
							compLevel = DSDA.CompatibilityLevel.Boom_Compatibility;
						}
						else
						{
							compLevel = DSDA.CompatibilityLevel.Boom_202;
						}
						break;
					case DemoVersion.MBF:
						if (portID == (byte) 'B') // "BOOM"
						{
							// don't advance!
							compLevel = DSDA.CompatibilityLevel.LxDoom;
						}
						else if (portID == (byte) 'M') // "MBF"
						{
							compLevel = DSDA.CompatibilityLevel.MBF21;
							i++;
						}
						break;
					case DemoVersion.PrBoom_2_1_0:
						compLevel = DSDA.CompatibilityLevel.PrBoom_2;
						i++;
						break;
					case DemoVersion.PrBoom_2_2_x:
						compLevel = DSDA.CompatibilityLevel.PrBoom_3;
						i++;
						break;
					case DemoVersion.PrBoom_2_3_x:
						compLevel = DSDA.CompatibilityLevel.PrBoom_4;
						i++;
						break;
					case DemoVersion.PrBoom_2_4_0:
						compLevel = DSDA.CompatibilityLevel.PrBoom_5;
						i++;
						break;
					case DemoVersion.PrBoomPlus:
						compLevel = DSDA.CompatibilityLevel.PrBoom_6;
						turningResolution = DSDA.TurningResolution.Longtics;
						i++;
						break;
					case DemoVersion.MBF21:
						compLevel = DSDA.CompatibilityLevel.MBF21;
						turningResolution = DSDA.TurningResolution.Longtics;
						i++;
						break;
					default:
						Result.Errors.Add($"Unknown demo format: {version}");
						return;
				}

				skill           = (DSDA.SkillLevel) (input[i++] + 1);
				episode         = input[i++];
				map             = input[i++];
				multiplayerMode = (DSDA.MultiplayerMode) input[i++];
				i++;    // DisplayPlayer is a non-sync setting so importers can't set it
				i += 6; // skip settings we can't parse yet
				monstersRespawn = input[i++] is not 0;
				fastMonsters    = input[i++] is not 0;
				noMonsters      = input[i++] is not 0;
				i++; // demo insurance
				rngSeed         = BinaryPrimitives.ReadUInt32BigEndian(input.AsSpan(i, 4));
				i = 0x4D;
				*/
			}

			DSDA.DoomSyncSettings syncSettings = new()
			{
				InputFormat = DSDA.ControllerTypes.Doom,
				CompatibilityLevel = compLevel,
				SkillLevel = skill,
				InitialEpisode = episode,
				InitialMap = map,
				MultiplayerMode = multiplayerMode,
				MonstersRespawn = monstersRespawn,
				FastMonsters = fastMonsters,
				NoMonsters = noMonsters,
				TurningResolution = turningResolution,
				RenderWipescreen = false,
				//RNGSeed = rngSeed,
			};

			syncSettings.Player1Present = input[i++] is not 0;
			syncSettings.Player2Present = input[i++] is not 0;
			syncSettings.Player3Present = input[i++] is not 0;
			syncSettings.Player4Present = input[i++] is not 0;
			/*
			if (compLevel >= DSDA.CompatibilityLevel.Boom_Compatibility
				&& version >= DemoVersion.Boom_2_00)
			{
				var FUTURE_MAXPLAYERS = 32;
				var g_maxplayers = 4;
				i += FUTURE_MAXPLAYERS - g_maxplayers;
			}
			*/
			Result.Movie.SyncSettingsJson = ConfigService.SaveWithType(syncSettings);

			var controller = new SimpleController(DSDA.CreateControllerDefinition(syncSettings));
			controller.Definition.BuildMnemonicsCache(Result.Movie.SystemID);
			Result.Movie.LogKey = Bk2LogEntryGenerator.GenerateLogKey(controller.Definition);

			void ParsePlayer(int port)
			{
				controller.AcceptNewAxis($"P{port} Run Speed",      unchecked((sbyte) input[i++]));
				controller.AcceptNewAxis($"P{port} Strafing Speed", unchecked((sbyte) input[i++]));
				if (turningResolution == DSDA.TurningResolution.Longtics)
				{
					// low byte comes first and is stored as an unsigned value
					controller.AcceptNewAxis($"P{port} Turning Speed Frac.", input[i++]);
				}
				controller.AcceptNewAxis($"P{port} Turning Speed", unchecked((sbyte) input[i++]));

				var buttons = input[i++];
				controller[$"P{port} Fire"] = (buttons & 0b00000001) is not 0;
				controller[$"P{port} Use"]  = (buttons & 0b00000010) is not 0;
				var changeWeapon            = (buttons & 0b00000100) is not 0;
				var weapon = changeWeapon ? (((buttons & 0b00111000) >> 3) + 1) : 0;
				controller.AcceptNewAxis($"P{port} Weapon Select", weapon);
			}

			do
			{
				if (syncSettings.Player1Present) ParsePlayer(1);
				if (syncSettings.Player2Present) ParsePlayer(2);
				if (syncSettings.Player3Present) ParsePlayer(3);
				if (syncSettings.Player4Present) ParsePlayer(4);

				Result.Movie.AppendFrame(controller);

				if (i == input.Length) throw new Exception("Reached end of input movie stream without finalization byte");
			}
			while (input[i] is not 0x80);
		}
	}
}
