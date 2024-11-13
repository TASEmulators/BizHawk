using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.PCEngine;

namespace BizHawk.Client.Common.movie.import
{
	// MC2 file format: http://code.google.com/p/pcejin/wiki/MC2
	// ReSharper disable once UnusedMember.Global
	[ImporterFor("PCEjin/Mednafen", ".mc2")]
	internal class Mc2Import : MovieImporter
	{
		private PceControllerDeck _deck;

		protected override void RunImport()
		{
			var ss = new PCEngine.PCESyncSettings
			{
				Port1 = PceControllerType.Unplugged,
				Port2 = PceControllerType.Unplugged,
				Port3 = PceControllerType.Unplugged,
				Port4 = PceControllerType.Unplugged,
				Port5 = PceControllerType.Unplugged
			};

			_deck = new PceControllerDeck(
				ss.Port1,
				ss.Port2,
				ss.Port3,
				ss.Port4,
				ss.Port5);

			Result.Movie.HeaderEntries[HeaderKeys.Platform] = VSystemID.Raw.PCE;
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
				else if (line.StartsWith("ports", StringComparison.OrdinalIgnoreCase))
				{
					var portNumStr = ParseHeader(line, "ports");
					if (int.TryParse(portNumStr, out int ports))
					{
						// Ugh
						if (ports > 0)
						{
							ss.Port1 = PceControllerType.GamePad;
						}

						if (ports > 1)
						{
							ss.Port2 = PceControllerType.GamePad;
						}

						if (ports > 2)
						{
							ss.Port3 = PceControllerType.GamePad;
						}

						if (ports > 3)
						{
							ss.Port4 = PceControllerType.GamePad;
						}

						if (ports > 4)
						{
							ss.Port5 = PceControllerType.GamePad;
						}

						_deck = new PceControllerDeck(
							ss.Port1,
							ss.Port2,
							ss.Port3,
							ss.Port4,
							ss.Port5);
					}
				}
				else if (line.StartsWith("pcecd", StringComparison.OrdinalIgnoreCase))
				{
					Result.Movie.HeaderEntries[HeaderKeys.Platform] = VSystemID.Raw.PCECD;
				}
				else if (line.StartsWith("emuversion", StringComparison.OrdinalIgnoreCase))
				{
					Result.Movie.Comments.Add($"{EmulationOrigin} Mednafen/PCEjin version {ParseHeader(line, "emuVersion")}");
				}
				else if (line.StartsWith("version", StringComparison.OrdinalIgnoreCase))
				{
					string version = ParseHeader(line, "version");
					Result.Movie.Comments.Add($"{MovieOrigin} .mc2 version {version}");
				}
				else if (line.StartsWith("romfilename", StringComparison.OrdinalIgnoreCase))
				{
					Result.Movie.HeaderEntries[HeaderKeys.GameName] = ParseHeader(line, "romFilename");
				}
				else if (line.StartsWith("cdgamename", StringComparison.OrdinalIgnoreCase))
				{
					Result.Movie.HeaderEntries[HeaderKeys.GameName] = ParseHeader(line, "cdGameName");
				}
				else if (line.StartsWith("comment author", StringComparison.OrdinalIgnoreCase))
				{
					Result.Movie.HeaderEntries[HeaderKeys.Author] = ParseHeader(line, "comment author");
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
				else
				{
					// Everything not explicitly defined is treated as a comment.
					Result.Movie.Comments.Add(line);
				}
			}

			Result.Movie.SyncSettingsJson = ConfigService.SaveWithType(ss);
		}

		// Import a frame from a text-based format.
		private void ImportTextFrame(string line)
		{
			var buttons = new[] { "Up", "Down", "Left", "Right", "B1", "B2", "Run", "Select" };

			_deck.Definition.BuildMnemonicsCache(Result.Movie.SystemID);
			SimpleController controllers = new(_deck.Definition);

			// Split up the sections of the frame.
			string[] sections = line.Split('|');

			/*
			 Skip the first two sections of the split, which consist of everything before the starting | and the command.
			 Do not use the section after the last |. In other words, get the sections for the players.
			*/
			int end = sections.Length - 1;

			for (int section = 2; section < end; section++)
			{
				// The player number is one less than the section number for the reasons explained above.
				int player = section - 1;
				string prefix = $"P{player} ";

				// Only count lines with that have the right number of buttons and are for valid players.
				if (
					sections[section].Length == buttons.Length)
				{
					for (int button = 0; button < buttons.Length; button++)
					{
						// Consider the button pressed so long as its spot is not occupied by a ".".
						controllers[prefix + buttons[button]] = sections[section][button] != '.';
					}
				}
			}

			// Convert the data for the controllers to a mnemonic and add it as a frame.
			Result.Movie.AppendFrame(controllers);
		}
	}
}
