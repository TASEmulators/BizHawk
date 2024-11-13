using System.Globalization;
using System.Linq;
using BizHawk.Common;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Consoles.Nintendo.NDS;

namespace BizHawk.Client.Common
{
	[ImporterFor("DeSmuME", ".dsm")]
	internal class DsmImport : MovieImporter
	{
		private static readonly ControllerDefinition DeSmuMEControllerDef = new ControllerDefinition("NDS Controller")
		{
			BoolButtons =
			{
				"Up", "Down", "Left", "Right", "Start", "Select", "B", "A", "Y", "X", "L", "R", "LidOpen", "LidClose", "Touch", "Microphone", "Power"
			}
		}.AddXYPair("Touch {0}", AxisPairOrientation.RightAndUp, 0.RangeTo(255), 128, 0.RangeTo(191), 96) //TODO verify direction against hardware
			.AddAxis("Mic Volume", 0.RangeTo(100), 100)
			.AddAxis("GBA Light Sensor", 0.RangeTo(10), 0)
			.MakeImmutable();

		protected override void RunImport()
		{
			Result.Movie.HeaderEntries[HeaderKeys.Platform] = VSystemID.Raw.NDS;

			var syncSettings = new NDS.NDSSyncSettings();

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
				else if (line.StartsWithOrdinal("rerecordCount"))
				{
					Result.Movie.Rerecords = (ulong) (int.TryParse(ParseHeader(line, "rerecordCount"), out var rerecordCount) ? rerecordCount : default);
				}
				else if (line.StartsWithOrdinal("firmNickname"))
				{
					syncSettings.FirmwareUsername = ParseHeader(line, "firmNickname");
				}
				else if (line.StartsWithOrdinal("firmFavColour"))
				{
					syncSettings.FirmwareFavouriteColour = (NDS.NDSSyncSettings.Color)byte.Parse(ParseHeader(line, "firmFavColour"));
				}
				else if (line.StartsWithOrdinal("firmBirthDay"))
				{
					syncSettings.FirmwareBirthdayDay = byte.Parse(ParseHeader(line, "firmBirthDay"));
				}
				else if (line.StartsWithOrdinal("firmBirthMonth"))
				{
					syncSettings.FirmwareBirthdayMonth = (NDS.NDSSyncSettings.Month)byte.Parse(ParseHeader(line, "firmBirthMonth"));
				}
				else if (line.StartsWithOrdinal("rtcStartNew"))
				{
					string rtcTime = ParseHeader(line, "rtcStartNew");
					syncSettings.InitialTime = DateTime.ParseExact(rtcTime, "yyyy'-'MMM'-'dd' 'HH':'mm':'ss':'fff", DateTimeFormatInfo.InvariantInfo);
				}
				else if (line.StartsWithOrdinal("comment author"))
				{
					Result.Movie.HeaderEntries[HeaderKeys.Author] = ParseHeader(line, "comment author");
				}
				else if (line.StartsWithOrdinal("comment"))
				{
					Result.Movie.Comments.Add(ParseHeader(line, "comment"));
				}
				else if (line.StartsWith("guid", StringComparison.OrdinalIgnoreCase))
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

		private readonly string[] _buttons = { "Right", "Left", "Down", "Up", "Start", "Select", "B", "A", "Y", "X", "L", "R", };

		private void ImportInputFrame(string line)
		{
			DeSmuMEControllerDef.BuildMnemonicsCache(Result.Movie.SystemID);
			SimpleController controller = new(DeSmuMEControllerDef);

			controller["LidOpen"] = false;
			controller["LidClose"] = false;
			controller["Microphone"] = false;
			controller["Power"] = false;

			string[] sections = line.Split(new[] {'|'}, StringSplitOptions.RemoveEmptyEntries);
			if (sections.Length > 0)
			{
				ProcessCmd(sections[0], controller);
			}

			if (sections.Length > 1)
			{
				var mnemonics = sections[1].Take(_buttons.Length).ToList();

				controller["Right"] = mnemonics[0] != '.';
				controller["Left"] = mnemonics[1] != '.';
				controller["Down"] = mnemonics[2] != '.';
				controller["Up"] = mnemonics[3] != '.';
				controller["Start"] = mnemonics[5] != '.'; // shoutouts to desmume doing start/select as select/start countary to docs
				controller["Select"] = mnemonics[4] != '.';
				controller["B"] = mnemonics[6] != '.';
				controller["A"] = mnemonics[7] != '.';
				controller["Y"] = mnemonics[8] != '.';
				controller["X"] = mnemonics[9] != '.';
				controller["L"] = mnemonics[10] != '.';
				controller["R"] = mnemonics[11] != '.';

				controller["Touch"] = sections[1].Substring(21, 1) != "0";

				var touchX = int.Parse(sections[1].Substring(13, 3));
				var touchY = int.Parse(sections[1].Substring(17, 3));

				controller.AcceptNewAxes(new[]
				{
					("Touch X", touchX),
					("Touch Y", touchY),
					("Mic Volume", 100),
					("GBA Light Sensor", 0),
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
