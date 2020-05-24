using System;

using BizHawk.Common;
using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Nintendo.NES;

namespace BizHawk.Client.Common
{
	// FM2 file format: http://www.fceux.com/web/FM2.html
	// ReSharper disable once UnusedMember.Global
	[ImporterFor("FCEUX", ".fm2")]
	internal class Fm2Import : MovieImporter
	{
		protected override void RunImport()
		{
			Result.Movie.HeaderEntries[HeaderKeys.Core] = CoreNames.NesHawk;
			const string emulator = "FCEUX";
			var platform = "NES"; // TODO: FDS?

			var syncSettings = new NES.NESSyncSettings();

			var controllerSettings = new NESControlSettings
			{
				NesLeftPort = nameof(UnpluggedNES),
				NesRightPort = nameof(UnpluggedNES)
			};

			_deck = controllerSettings.Instantiate((x, y) => true);
			AddDeckControlButtons();

			Result.Movie.HeaderEntries[HeaderKeys.Platform] = platform;

			using var sr = SourceFile.OpenText();
			string line;
			while ((line = sr.ReadLine()) != null)
			{
				if (line == "")
				{
					continue;
				}

				if (line[0] == '|')
				{
					ImportInputFrame(line);
				}
				else if (line.ToLower().StartsWith("sub"))
				{
					var subtitle = ImportTextSubtitle(line);

					if (!string.IsNullOrEmpty(subtitle))
					{
						Result.Movie.Subtitles.AddFromString(subtitle);
					}
				}
				else if (line.ToLower().StartsWith("emuversion"))
				{
					Result.Movie.Comments.Add($"{EmulationOrigin} {emulator} version {ParseHeader(line, "emuVersion")}");
				}
				else if (line.ToLower().StartsWith("version"))
				{
					string version = ParseHeader(line, "version");

					if (version != "3")
					{
						Result.Warnings.Add("Detected a .fm2 movie version other than 3, which is unsupported");
					}
					else
					{
						Result.Movie.Comments.Add($"{MovieOrigin} .fm2 version 3");
					}
				}
				else if (line.ToLower().StartsWith("romfilename"))
				{
					Result.Movie.HeaderEntries[HeaderKeys.GameName] = ParseHeader(line, "romFilename");
				}
				else if (line.ToLower().StartsWith("cdgamename"))
				{
					Result.Movie.HeaderEntries[HeaderKeys.GameName] = ParseHeader(line, "cdGameName");
				}
				else if (line.ToLower().StartsWith("romchecksum"))
				{
					string blob = ParseHeader(line, "romChecksum");
					byte[] md5 = DecodeBlob(blob);
					if (md5 != null && md5.Length == 16)
					{
						Result.Movie.HeaderEntries[Md5] = md5.BytesToHexString().ToLower();
					}
					else
					{
						Result.Warnings.Add("Bad ROM checksum.");
					}
				}
				else if (line.ToLower().StartsWith("comment author"))
				{
					Result.Movie.HeaderEntries[HeaderKeys.Author] = ParseHeader(line, "comment author");
				}
				else if (line.ToLower().StartsWith("rerecordcount"))
				{
					int.TryParse(ParseHeader(line, "rerecordCount"), out var rerecordCount);
					Result.Movie.Rerecords = (ulong)rerecordCount;
				}
				else if (line.ToLower().StartsWith("guid"))
				{
					// We no longer care to keep this info
				}
				else if (line.ToLower().StartsWith("startsfromsavestate"))
				{
					// If this movie starts from a savestate, we can't support it.
					if (ParseHeader(line, "StartsFromSavestate") == "1")
					{
						Result.Errors.Add("Movies that begin with a savestate are not supported.");
						break;
					}
				}
				else if (line.ToLower().StartsWith("palflag"))
				{
					Result.Movie.HeaderEntries[HeaderKeys.Pal] = ParseHeader(line, "palFlag");
				}
				else if (line.ToLower().StartsWith("port0"))
				{
					if (ParseHeader(line, "port0") == "1")
					{
						controllerSettings.NesLeftPort = nameof(ControllerNES);
						_deck = controllerSettings.Instantiate((x, y) => false);
						AddDeckControlButtons();
					}
				}
				else if (line.ToLower().StartsWith("port1"))
				{
					if (ParseHeader(line, "port1") == "1")
					{
						controllerSettings.NesRightPort = nameof(ControllerNES);
						_deck = controllerSettings.Instantiate((x, y) => false);
						AddDeckControlButtons();
					}
				}
				else if (line.ToLower().StartsWith("port2"))
				{
					if (ParseHeader(line, "port2") == "1")
					{
						Result.Errors.Add("Famicom port not yet supported");
						break;
					}
				}
				else if (line.ToLower().StartsWith("fourscore"))
				{
					bool fourscore = ParseHeader(line, "fourscore") == "1";
					if (fourscore)
					{
						// TODO: set controller config sync settings
						controllerSettings.NesLeftPort = nameof(FourScore);
						controllerSettings.NesRightPort = nameof(FourScore);
					}

					_deck = controllerSettings.Instantiate((x, y) => false);
				}
				else
				{
					Result.Movie.Comments.Add(line); // Everything not explicitly defined is treated as a comment.
				}
			}

			syncSettings.Controls = controllerSettings;
			Result.Movie.SyncSettingsJson = ConfigService.SaveWithType(syncSettings);
		}

		private IControllerDeck _deck;
		
		private readonly string[] _buttons = { "Right", "Left", "Down", "Up", "Start", "Select", "B", "A" };
		private void ImportInputFrame(string line)
		{
			var controllers = new SimpleController
			{
				Definition = _deck.GetDefinition()
			};

			string[] sections = line.Split(new[] {'|'}, StringSplitOptions.RemoveEmptyEntries);
			controllers["Reset"] = sections[1][0] == '1';
			switch (sections[0][0])
			{
				case '0':
					break;
				case '1':
					controllers["Reset"] = true;
					break;
				case '2':
					controllers["Power"] = true;
					break;
				case '4':
					controllers["FDS Insert 0"] = true;
					break;
				case '8':
					controllers["FDS Insert 1"] = true;
					break;
				// TODO: insert coin?
				default:
					Result.Warnings.Add($"Unknown command: {sections[0][0]}");
					break;
			}

			for (int player = 1; player < sections.Length; player++)
			{
				string prefix = $"P{player} ";
				// Only count lines with that have the right number of buttons and are for valid players.
				if (sections[player].Length == _buttons.Length)
				{
					for (int button = 0; button < _buttons.Length; button++)
					{
						// Consider the button pressed so long as its spot is not occupied by a ".".
						controllers[prefix + _buttons[button]] = sections[player][button] != '.';
					}
				}
			}

			Result.Movie.AppendFrame(controllers);
		}

		private void AddDeckControlButtons()
		{
			var controllers = new SimpleController
			{
				Definition = _deck.GetDefinition()
			};

			// TODO: FDS
			// Yes, this adds them to the deck definition too
			controllers.Definition.BoolButtons.Add("Reset");
			controllers.Definition.BoolButtons.Add("Power");
		}

		private static string ImportTextSubtitle(string line)
		{
			line = SingleSpaces(line);

			// The header name, frame, and message are separated by whitespace.
			int first = line.IndexOf(' ');
			int second = line.IndexOf(' ', first + 1);
			if (first != -1 && second != -1)
			{
				// Concatenate the frame and message with default values for the additional fields.
				string frame = line.Substring(0, first);
				string length = line.Substring(first + 1, second - first - 1);
				string message = line.Substring(second + 1).Trim();

				return $"subtitle {frame} 0 0 {length} FFFFFFFF {message}";
			}

			return null;
		}

		// Decode a blob used in FM2 (base64:..., 0x123456...)
		private static byte[] DecodeBlob(string blob)
		{
			if (blob.Length < 2)
			{
				return null;
			}

			if (blob[0] == '0' && (blob[1] == 'x' || blob[1] == 'X'))
			{
				// hex
				return blob.Substring(2).HexStringToBytes();
			}

			// base64
			if (!blob.ToLower().StartsWith("base64:"))
			{
				return null;
			}

			try
			{
				return Convert.FromBase64String(blob.Substring(7));
			}
			catch (FormatException)
			{
				return null;
			}
		}
	}
}
