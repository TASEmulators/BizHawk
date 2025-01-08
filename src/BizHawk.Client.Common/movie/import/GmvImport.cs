using System.IO;
using System.Text;

using BizHawk.Common.CollectionExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Consoles.Sega.gpgx;

namespace BizHawk.Client.Common.movie.import
{
	// GMV file format: http://code.google.com/p/gens-rerecording/wiki/GMV
	// ReSharper disable once UnusedMember.Global
	[ImporterFor("GENS", ".gmv")]
	internal class GmvImport : MovieImporter
	{
		protected override void RunImport()
		{
			using var fs = SourceFile.Open(FileMode.Open, FileAccess.Read);
			using var r = new BinaryReader(fs, Encoding.ASCII);
			
			// 000 16-byte signature and format version: "Gens Movie TEST9"
			byte[] signature = r.ReadBytes(15);
			if (!signature.SequenceEqual("Gens Movie TEST"u8))
			{
				Result.Errors.Add("This is not a valid .GMV file.");
				return;
			}

			Result.Movie.HeaderEntries[HeaderKeys.Platform] = VSystemID.Raw.GEN;

			// 00F ASCII-encoded GMV file format version. The most recent is 'A'. (?)
			char version = r.ReadChar();
			Result.Movie.Comments.Add($"{MovieOrigin} .GMV version {version}");
			Result.Movie.Comments.Add($"{EmulationOrigin} Gens");

			// 010 4-byte little-endian unsigned int: rerecord count
			uint rerecordCount = r.ReadUInt32();
			Result.Movie.Rerecords = rerecordCount;

			
			// 014 ASCII-encoded controller config for player 1. '3' or '6'.
			char player1Config = r.ReadChar();

			// 015 ASCII-encoded controller config for player 2. '3' or '6'.
			char player2Config = r.ReadChar();

			// 016 special flags (Version A and up only)
			byte flags = r.ReadByte();

			/*
			 bit 7 (most significant): if "1", movie runs at 50 frames per second; if "0", movie runs at 60 frames per
			 second The file format has no means of identifying NTSC/"PAL", but the FPS can still be derived from the
			 header.
			*/
			bool pal = ((flags >> 7) & 0x1) != 0;
			Result.Movie.HeaderEntries[HeaderKeys.Pal] = pal.ToString();

			// bit 6: if "1", movie requires a savestate.
			if (((flags >> 6) & 0x1) != 0)
			{
				Result.Errors.Add("Movies that begin with a savestate are not supported.");
				return;
			}

			// bit 5: if "1", movie is 3-player movie; if "0", movie is 2-player movie
			bool threePlayers = ((flags >> 5) & 0x1) != 0;

			bool useSixButtons = !threePlayers && (player1Config == '6' || player2Config == '6');

			LibGPGX.InputData input = new LibGPGX.InputData();
			input.dev[0] = useSixButtons
				? LibGPGX.INPUT_DEVICE.DEVICE_PAD6B
				: LibGPGX.INPUT_DEVICE.DEVICE_PAD3B;

			input.dev[1] = useSixButtons
				? LibGPGX.INPUT_DEVICE.DEVICE_PAD6B
				: LibGPGX.INPUT_DEVICE.DEVICE_PAD3B;

			var ss = new GPGX.GPGXSyncSettings
			{
				UseSixButton = useSixButtons,
				ControlTypeLeft = GPGX.ControlType.Normal,
				ControlTypeRight = GPGX.ControlType.Normal
			};

			input.dev[2] = input.dev[3] = input.dev[4] = input.dev[5] = input.dev[6] = input.dev[7] = LibGPGX.INPUT_DEVICE.DEVICE_NONE;

			if (threePlayers)
			{
				input.dev[2] = LibGPGX.INPUT_DEVICE.DEVICE_PAD3B;
			}

			GPGXControlConverter controlConverter = new(input, systemId: VSystemID.Raw.GEN, cdButtons: false);

			controlConverter.ControllerDef.BuildMnemonicsCache(Result.Movie.SystemID);
			SimpleController controller = new(controlConverter.ControllerDef);

			Result.Movie.LogKey = Bk2LogEntryGenerator.GenerateLogKey(controlConverter.ControllerDef);

			// Unknown.
			r.ReadByte();

			// 018 40-byte zero-terminated ASCII movie name string
			string description = NullTerminated(new string(r.ReadChars(40)));
			Result.Movie.Comments.Add(description);

			/*
			 040 frame data
			 For controller bytes, each value is determined by OR-ing together values for whichever of the following are
			 left unpressed:
			 * 0x01 Up
			 * 0x02 Down
			 * 0x04 Left
			 * 0x08 Right
			 * 0x10 A
			 * 0x20 B
			 * 0x40 C
			 * 0x80 Start
			*/
			string[] buttons = { "Up", "Down", "Left", "Right", "A", "B", "C", "Start" };
			/*
			 For XYZ-mode, each value is determined by OR-ing together values for whichever of the following are left
			 unpressed:
			 * 0x01 Controller 1 X
			 * 0x02 Controller 1 Y
			 * 0x04 Controller 1 Z
			 * 0x08 Controller 1 Mode
			 * 0x10 Controller 2 X
			 * 0x20 Controller 2 Y
			 * 0x40 Controller 2 Z
			 * 0x80 Controller 2 Mode
			*/
			string[] other = { "X", "Y", "Z", "Mode" };

			// The file has no terminator byte or frame count. The number of frames is the <filesize minus 64> divided by 3.
			long frameCount = (fs.Length - 64) / 3;
			for (long frame = 1; frame <= frameCount; frame++)
			{
				// Each frame consists of 3 bytes.
				for (int player = 1; player <= 3; player++)
				{
					byte controllerState = r.ReadByte();

					// * is controller 3 if a 3-player movie, or XYZ-mode if a 2-player movie.
					if (player != 3 || threePlayers)
					{
						for (int button = 0; button < buttons.Length; button++)
						{
							controller[$"P{player} {buttons[button]}"] = ((controllerState >> button) & 0x1) == 0;
						}
					}
					else
					{
						for (int button = 0; button < other.Length; button++)
						{
							if (player1Config == '6')
							{
								controller[$"P1 {other[button]}"] = ((controllerState >> button) & 0x1) == 0;
							}

							if (player2Config == '6')
							{
								controller[$"P2 {other[button]}"] = ((controllerState >> (button + 4)) & 0x1) == 0;
							}
						}
					}
				}

				Result.Movie.AppendFrame(controller);
			}

			Result.Movie.SyncSettingsJson = ConfigService.SaveWithType(ss);
			Result.Movie.HeaderEntries[HeaderKeys.Core] = CoreNames.Gpgx;
		}
	}
}
