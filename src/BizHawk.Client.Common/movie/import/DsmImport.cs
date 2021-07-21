using System;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Consoles.Nintendo.NDS;

namespace BizHawk.Client.Common
{
	[ImporterFor("DeSmuME", ".dsm")]
	internal class DsmImport : MovieImporter
	{
		protected override void RunImport()
		{
			Result.Movie.HeaderEntries[HeaderKeys.Platform] = "NDS";

			var syncSettings = new MelonDS.MelonSyncSettings();

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
					ImportInputFrame(line);
				}
				else if (line.StartsWith("rerecordCount"))
				{
					int.TryParse(ParseHeader(line, "rerecordCount"), out var rerecordCount);
					Result.Movie.Rerecords = (ulong)rerecordCount;
				}
				else if (line.StartsWith("firmNickname"))
				{
					syncSettings.Nickname = ParseHeader(line, "firmNickname");
				}
				else if (line.StartsWith("firmFavColour"))
				{
					syncSettings.FavoriteColor = byte.Parse(ParseHeader(line, "firmFavColour"));
				}
				else if (line.StartsWith("firmBirthDay"))
				{
					syncSettings.BirthdayDay = byte.Parse(ParseHeader(line, "firmBirthDay"));
				}
				else if (line.StartsWith("firmBirthMonth"))
				{
					syncSettings.BirthdayMonth = byte.Parse(ParseHeader(line, "firmBirthMonth"));
				}
				else if (line.StartsWith("rtcStartNew"))
				{
					//TODO: what is this format?? 2010-JAN-01 00:00:00:000
					//var time = DateTime.Parse(ParseHeader(line, "rtcStartNew"));
					//syncSettings.TimeAtBoot = (uint)new DateTimeOffset(time.ToLocalTime()).ToUnixTimeSeconds();
				}
				else if (line.StartsWith("comment author"))
				{
					Result.Movie.HeaderEntries[HeaderKeys.Author] = ParseHeader(line, "comment author");
				}
				else if (line.StartsWith("comment"))
				{
					Result.Movie.Comments.Add(ParseHeader(line, "comment"));
				}
				else if (line.ToLower().StartsWith("guid"))
				{
					// We no longer care to keep this info
				}
				else
				{
					Result.Movie.Comments.Add(line); // Everything not explicitly defined is treated as a comment.
				}

				Result.Movie.SyncSettingsJson = ConfigService.SaveWithType(syncSettings);
			}

			Result.Movie.HeaderEntries[HeaderKeys.Core] = CoreNames.MelonDS;
		}

		private readonly string[] _buttons = { "Left", "Right", "Up", "Down", "A", "B", "X", "Y", "L", "R", "Start", "Select" };

		private void ImportInputFrame(string line)
		{
			var controller = new SimpleController
			{
				Definition = new ControllerDefinition
				{
					BoolButtons =
					{
						"Left", "Right", "Up", "Down",
						"A", "B", "X", "Y", "L", "R", "Start", "Select", "LidOpen", "LidClose", "Power", "Touch"
					}
				}.AddXYPair("Touch{0}", AxisPairOrientation.RightAndUp, 0.RangeTo(255), 128, 0.RangeTo(191), 96) //TODO verify direction against hardware
			};

			controller["LidOpen"] = false;
			controller["LidClose"] = false;
			controller["Power"] = false;

			string[] sections = line.Split(new[] {'|'}, StringSplitOptions.RemoveEmptyEntries);
			if (sections.Length > 0)
			{
				ProcessCmd(sections[0], controller);
			}
			
			if (sections.Length > 1)
			{
				var mnemonics = sections[1].Take(_buttons.Length).ToList();

				controller["Left"] = mnemonics[1] != '.';
				controller["Right"] = mnemonics[0] != '.';
				controller["Up"] = mnemonics[3] != '.';
				controller["Down"] = mnemonics[2] != '.';
				controller["A"] = mnemonics[7] != '.';
				controller["B"] = mnemonics[6] != '.';
				controller["X"] = mnemonics[9] != '.';
				controller["Y"] = mnemonics[8] != '.';
				controller["L"] = mnemonics[10] != '.';
				controller["R"] = mnemonics[11] != '.';
				controller["Start"] = mnemonics[4] != '.';
				controller["Select"] = mnemonics[5] != '.';

				controller["Touch"] = sections[1].Substring(21, 1) != "0";

				var touchX = int.Parse(sections[1].Substring(13, 3));
				var touchY = int.Parse(sections[1].Substring(17, 3));

				controller.AcceptNewAxes(new[]
				{
					("TouchX", touchX),
					("TouchY", touchY)
				});
			}

			Result.Movie.AppendFrame(controller);
		}

		private void ProcessCmd(string cmd, SimpleController controller)
		{
			// TODO
		}
	}
}
