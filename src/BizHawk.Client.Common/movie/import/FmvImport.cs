using System.IO;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.NES;

namespace BizHawk.Client.Common.movie.import
{
	/// <summary>For Famtasia's <see href="https://tasvideos.org/EmulatorResources/Famtasia/FMV"><c>.fmv</c> format</see></summary>
	[ImporterFor("Famtasia", ".fmv")]
	internal class FmvImport : MovieImporter
	{
		private IControllerDeck _deck;

		protected override void RunImport()
		{
			using var fs = SourceFile.Open(FileMode.Open, FileAccess.Read);
			using var r = new BinaryReader(fs);
			// 000 4-byte signature: 46 4D 56 1A "FMV\x1A"
			var signature = new string(r.ReadChars(4));
			if (signature != "FMV\x1A")
			{
				Result.Errors.Add("This is not a valid .FMV file.");
				return;
			}

			// 004 1-byte flags:
			byte flags = r.ReadByte();

			// bit 7: 0=reset-based, 1=savestate-based
			if (((flags >> 2) & 0x1) != 0)
			{
				Result.Errors.Add("Movies that begin with a savestate are not supported.");
				return;
			}

			Result.Movie.HeaderEntries[HeaderKeys.Platform] = VSystemID.Raw.NES;
			var syncSettings = new NES.NESSyncSettings();

			// other bits: unknown, set to 0
			// 005 1-byte flags:
			flags = r.ReadByte();

			// bit 5: is a FDS recording
			bool fds;
			if (((flags >> 5) & 0x1) != 0)
			{
				fds = true;
				Result.Movie.HeaderEntries[HeaderKeys.BoardName] = "FDS";
			}
			else
			{
				fds = false;
			}

			// bit 6: uses controller 2
			bool controller2 = ((flags >> 6) & 0x1) != 0;

			// bit 7: uses controller 1
			bool controller1 = ((flags >> 7) & 0x1) != 0;

			// other bits: unknown, set to 0
			// 006 4-byte little-endian unsigned int: unknown, set to 00000000
			r.ReadInt32();

			// 00A 4-byte little-endian unsigned int: rerecord count minus 1
			uint rerecordCount = r.ReadUInt32();

			/*
			 The rerecord count stored in the file is the number of times a savestate was loaded. If a savestate was never
			 loaded, the number is 0. Famtasia however displays "1" in such case. It always adds 1 to the number found in
			 the file.
			*/
			Result.Movie.Rerecords = rerecordCount + 1;

			// 00E 2-byte little-endian unsigned int: unknown, set to 0000
			r.ReadInt16();

			// 010 64-byte zero-terminated emulator identifier string
			string emuVersion = NullTerminated(new string(r.ReadChars(64)));
			Result.Movie.Comments.Add($"{EmulationOrigin} Famtasia version {emuVersion}");
			Result.Movie.Comments.Add($"{MovieOrigin} .FMV");

			// 050 64-byte zero-terminated movie title string
			string description = NullTerminated(new string(r.ReadChars(64)));
			Result.Movie.Comments.Add(description);
			if (!controller1 && !controller2 && !fds)
			{
				Result.Warnings.Add("No input recorded.");
			}

			var controllerSettings = new NESControlSettings
			{
				NesLeftPort = controller1 ? nameof(ControllerNES) : nameof(UnpluggedNES),
				NesRightPort = controller2 ? nameof(ControllerNES) : nameof(UnpluggedNES)
			};
			_deck = controllerSettings.Instantiate((x, y) => true).AddSystemToControllerDef();
			_deck.ControllerDef.BuildMnemonicsCache(Result.Movie.SystemID);
			syncSettings.Controls.NesLeftPort = controllerSettings.NesLeftPort;
			syncSettings.Controls.NesRightPort = controllerSettings.NesRightPort;

			SimpleController controllers = new(_deck.ControllerDef);

			/*
			 * 01 Right
			 * 02 Left
			 * 04 Up
			 * 08 Down
			 * 10 B
			 * 20 A
			 * 40 Select
			 * 80 Start
			*/
			string[] buttons = { "Right", "Left", "Up", "Down", "B", "A", "Select", "Start" };
			bool[] masks = { controller1, controller2, fds };
			/*
			 The file has no terminator byte or frame count. The number of frames is the <filesize minus 144> divided by
			 <number of bytes per frame>.
			*/
			int bytesPerFrame = 0;
			for (int player = 1; player <= masks.Length; player++)
			{
				if (masks[player - 1])
				{
					bytesPerFrame++;
				}
			}

			long frameCount = (fs.Length - 144) / bytesPerFrame;
			for (long frame = 1; frame <= frameCount; frame++)
			{
				/*
				 Each frame consists of 1 or more bytes. Controller 1 takes 1 byte, controller 2 takes 1 byte, and the FDS
				 data takes 1 byte. If all three exist, the frame is 3 bytes. For example, if the movie is a regular NES game
				 with only controller 1 data, a frame is 1 byte.
				*/
				for (int player = 1; player <= masks.Length; player++)
				{
					if (!masks[player - 1])
					{
						continue;
					}

					byte controllerState = r.ReadByte();
					if (player != 3)
					{
						for (int button = 0; button < buttons.Length; button++)
						{
							controllers[$"P{player} {buttons[button]}"] = ((controllerState >> button) & 0x1) != 0;
						}
					}
					else
					{
						Result.Warnings.Add("FDS commands are not properly supported.");
					}
				}

				Result.Movie.AppendFrame(controllers);
			}

			Result.Movie.SyncSettingsJson = ConfigService.SaveWithType(syncSettings);
		}
	}
}
