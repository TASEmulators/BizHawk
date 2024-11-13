using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common.movie.import
{
	// https://code.google.com/archive/p/yabause-rr/wikis/YMVfileformat.wiki
	// ReSharper disable once UnusedMember.Global
	[ImporterFor("Yabause", ".ymv")]
	internal class YmvImport : MovieImporter
	{
		protected override void RunImport()
		{
			Result.Movie.HeaderEntries[HeaderKeys.Platform] = VSystemID.Raw.SAT;
			var ss = new Emulation.Cores.Waterbox.NymaCore.NymaSyncSettings
			{
				PortDevices =
				{
					[0] = "gamepad",
					[1] = "none",
					[2] = "none",
					[3] = "none",
					[4] = "none",
					[5] = "none",
					[6] = "none",
					[7] = "none",
					[8] = "none",
					[9] = "none",
					[10] = "none",
					[11] = "none",
				}
			};

			using var sr = SourceFile.OpenText();
			string line;
			while ((line = sr.ReadLine()) != null)
			{
				if (string.IsNullOrWhiteSpace(line))
				{
					continue;
				}

				if (line[0] == '|')
				{
					ImportTextFrame(line);
				}
				else if (line.StartsWith("emuversion", StringComparison.OrdinalIgnoreCase))
				{
					Result.Movie.Comments.Add($"{EmulationOrigin} Yabause version {ParseHeader(line, "emuVersion")}");
				}
				else if (line.StartsWith("version", StringComparison.OrdinalIgnoreCase))
				{
					string version = ParseHeader(line, "version");
					Result.Movie.Comments.Add($"{MovieOrigin} .ymv version {version}");
				}
				else if (line.StartsWith("cdGameName", StringComparison.OrdinalIgnoreCase))
				{
					Result.Movie.HeaderEntries[HeaderKeys.GameName] = ParseHeader(line, "romFilename");
				}
				else if (line.StartsWith("rerecordcount", StringComparison.OrdinalIgnoreCase))
				{
					int rerecordCount;

					// Try to parse the re-record count as an integer, defaulting to 0 if it fails.
					try
					{
						rerecordCount = int.Parse(ParseHeader(line, "rerecordCount"));
					}
					catch
					{
						rerecordCount = 0;
					}

					Result.Movie.Rerecords = (ulong)rerecordCount;
				}
				else if (line.StartsWith("startsfromsavestate", StringComparison.OrdinalIgnoreCase))
				{
					// If this movie starts from a savestate, we can't support it.
					if (ParseHeader(line, "StartsFromSavestate") == "1")
					{
						Result.Errors.Add("Movies that begin with a savestate are not supported.");
					}
				}
				else if (line.StartsWith("ispal", StringComparison.OrdinalIgnoreCase))
				{
					bool pal = ParseHeader(line, "isPal") == "1";
					Result.Movie.HeaderEntries[HeaderKeys.Pal] = pal.ToString();
				}
				else
				{
					// Everything not explicitly defined is treated as a comment.
					Result.Movie.Comments.Add(line);
				}
			}

			Result.Movie.SyncSettingsJson = ConfigService.SaveWithType(ss);
		}

		private void ImportTextFrame(string line)
		{
			// Yabause only supported 1 controller
			SimpleController controllers = new(new ControllerDefinition("Saturn Controller")
			{
				BoolButtons = new List<string>
				{
					"Reset", "Power", "Previous Disk", "Next Disk", "P1 Left", "P1 Right", "P1 Up", "P1 Down", "P1 Start", "P1 A", "P1 B", "P1 C", "P1 X", "P1 Y", "P1 Z", "P1 L", "P1 R"
				}
			}.MakeImmutable());
			controllers.Definition.BuildMnemonicsCache(Result.Movie.SystemID);

			// Split up the sections of the frame.
			var sections = line.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
			if (sections.Length != 2)
			{
				Result.Errors.Add("Unsupported input configuration");
				return;
			}

			if (sections[0][0] == '1')
			{
				controllers["Reset"] = true;
			}

			var buttonNames = controllers.Definition.ControlsOrdered[1];

			// Only count lines with that have the right number of buttons and are for valid players.
			if (sections[1].Length == buttonNames.Count)
			{
				for (int button = 0; button < buttonNames.Count; button++)
				{
					// Consider the button pressed so long as its spot is not occupied by a ".".
					controllers[buttonNames[button].Name] = sections[1][button] != '.';
				}
			}

			// Convert the data for the controllers to a mnemonic and add it as a frame.
			Result.Movie.AppendFrame(controllers);
		}

	}
}
