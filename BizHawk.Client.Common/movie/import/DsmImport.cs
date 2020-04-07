using System;
using System.Linq;
using BizHawk.Common;
using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;
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
		}

		private readonly string[] _buttons = { "Right", "Left", "Down", "Up", "Start", "Select", "B", "A", "Y", "X", "L", "R" };

		private void ImportInputFrame(string line)
		{
			var controller = new SimpleController
			{
				Definition = new ControllerDefinition
				{
					BoolButtons =
					{
						"Right", "Left", "Down", "Up", "Start", "Select",
						"B", "A", "X", "Y", "L", "R", "LidOpen", "LidClose", "Touch"
					}
				}
			};

			controller.Definition.AxisControls.Add("TouchX");
			controller.Definition.AxisRanges.Add(new ControllerDefinition.AxisRange(0, 128, 255));
			controller.Definition.AxisControls.Add("TouchY");
			controller.Definition.AxisRanges.Add(new ControllerDefinition.AxisRange(0, 96, 191));

			controller["LidOpen"] = false;
			controller["LidOpen"] = false;

			string[] sections = line.Split(new[] {'|'}, StringSplitOptions.RemoveEmptyEntries);
			if (sections.Length > 0)
			{
				ProcessCmd(sections[0], controller);
			}
			
			if (sections.Length > 1)
			{
				var mnemonics = sections[1].Take(_buttons.Length).ToList();
				for (var i = 0; i < mnemonics.Count; i++)
				{
					controller[_buttons[i]] = mnemonics[i] != '.';
				}

				controller["Touch"] = sections[1].Substring(21, 1) != "0";

				var touchX = int.Parse(sections[1].Substring(13, 3));
				var touchY = int.Parse(sections[1].Substring(17, 3));

				controller.AcceptNewAxes(new[]
				{
					("TouchX", (float) touchX),
					("TouchY", (float) touchY)
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
