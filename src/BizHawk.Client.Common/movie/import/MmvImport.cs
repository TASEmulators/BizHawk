using System.IO;
using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Sega.MasterSystem;

namespace BizHawk.Client.Common.movie.import
{
	/// <summary>For Dega's <see href="https://tasvideos.org/EmulatorResources/MMV"><c>.mmv</c> format</see></summary>
	[ImporterFor("Dega", ".mmv")]
	internal class MmvImport : MovieImporter
	{
		protected override void RunImport()
		{
			using var fs = SourceFile.Open(FileMode.Open, FileAccess.Read);
			using var r = new BinaryReader(fs);

			// 0000: 4-byte signature: "MMV\0"
			string signature = new string(r.ReadChars(4));
			if (signature != "MMV\0")
			{
				Result.Errors.Add("This is not a valid .MMV file.");
				return;
			}

			// 0004: 4-byte little endian unsigned int: dega version
			uint emuVersion = r.ReadUInt32();
			Result.Movie.Comments.Add($"{MovieOrigin} .MMV");
			Result.Movie.Comments.Add($"{EmulationOrigin} Dega version {emuVersion}");

			// 0008: 4-byte little endian unsigned int: frame count
			uint frameCount = r.ReadUInt32();

			// 000c: 4-byte little endian unsigned int: rerecord count
			uint rerecordCount = r.ReadUInt32();
			Result.Movie.Rerecords = rerecordCount;


			// 0010: 4-byte little endian flag: begin from reset?
			uint reset = r.ReadUInt32();
			if (reset == 0)
			{
				Result.Errors.Add("Movies that begin with a savestate are not supported.");
				return;
			}

			// 0014: 4-byte little endian unsigned int: offset of state information
			r.ReadUInt32();

			// 0018: 4-byte little endian unsigned int: offset of input data
			r.ReadUInt32();

			// 001c: 4-byte little endian unsigned int: size of input packet
			r.ReadUInt32();

			// 0020-005f: string: author info (UTF-8)
			string author = NullTerminated(new string(r.ReadChars(64)));
			Result.Movie.HeaderEntries[HeaderKeys.Author] = author;

			// 0060: 4-byte little endian flags
			byte flags = r.ReadByte();

			// bit 0: unused
			// bit 1: "PAL"
			bool pal = ((flags >> 1) & 0x1) != 0;
			Result.Movie.HeaderEntries[HeaderKeys.Pal] = pal.ToString();

			// bit 2: Japan
			bool japan = ((flags >> 2) & 0x1) != 0;
			Result.Movie.HeaderEntries["Japan"] = japan.ToString();

			// bit 3: Game Gear (version 1.16+)
			bool isGameGear;
			if (((flags >> 3) & 0x1) != 0)
			{
				isGameGear = true;
				Result.Movie.HeaderEntries.Add("IsGGMode", "1");
			}
			else
			{
				isGameGear = false;
			}

			Result.Movie.HeaderEntries[HeaderKeys.Platform] = VSystemID.Raw.SMS; // System Id is still SMS even if game gear

			// bits 4-31: unused
			r.ReadBytes(3);

			// 0064-00e3: string: rom name (ASCII)
			string gameName = NullTerminated(new string(r.ReadChars(128)));
			Result.Movie.HeaderEntries[HeaderKeys.GameName] = gameName;

			// 00e4-00f3: binary: rom MD5 digest
			byte[] md5 = r.ReadBytes(16);
			Result.Movie.HeaderEntries[HeaderKeys.Md5] = md5.BytesToHexString().ToLowerInvariant();

			var ss = new SMS.SmsSyncSettings();
			var cd = new SMSControllerDeck(ss.Port1, ss.Port2, isGameGear, ss.UseKeyboard);
			cd.Definition.BuildMnemonicsCache(Result.Movie.SystemID);
			SimpleController controllers = new(cd.Definition);

			/*
			 76543210
			 * bit 0 (0x01): up
			 * bit 1 (0x02): down
			 * bit 2 (0x04): left
			 * bit 3 (0x08): right
			 * bit 4 (0x10): 1
			 * bit 5 (0x20): 2
			 * bit 6 (0x40): start (Master System)
			 * bit 7 (0x80): start (Game Gear)
			*/
			string[] buttons = { "Up", "Down", "Left", "Right", "B1", "B2" };
			for (int frame = 1; frame <= frameCount; frame++)
			{
				/*
				 Controller data is made up of one input packet per frame. Each packet currently consists of 2 bytes. The
				 first byte is for controller 1 and the second controller 2. The Game Gear only uses the controller 1 input
				 however both bytes are still present.
				*/
				for (int player = 1; player <= 2; player++)
				{
					byte controllerState = r.ReadByte();
					for (int button = 0; button < buttons.Length; button++)
					{
						controllers[$"P{player} {buttons[button]}"] = ((controllerState >> button) & 0x1) != 0;
					}

					if (player == 1)
					{
						controllers["Pause"] =
							(((controllerState >> 6) & 0x1) != 0 && !isGameGear)
							|| (((controllerState >> 7) & 0x1) != 0 && isGameGear);
					}
				}

				Result.Movie.AppendFrame(controllers);
			}

			Result.Movie.SyncSettingsJson = ConfigService.SaveWithType(ss);
		}
	}
}
