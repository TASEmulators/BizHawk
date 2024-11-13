using System.Collections.Generic;
using System.IO;
using System.Text;
using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Nintendo.NES;

namespace BizHawk.Client.Common.movie.import
{
	// FCM file format: http://code.google.com/p/fceu/wiki/FCM
	// ReSharper disable once UnusedMember.Global
	[ImporterFor("FCEU", ".fcm")]
	internal class FcmImport : MovieImporter
	{
		private IControllerDeck _deck;

		protected override void RunImport()
		{
			Result.Movie.HeaderEntries[HeaderKeys.Core] = CoreNames.NesHawk;

			using var r = new BinaryReader(SourceFile.Open(FileMode.Open, FileAccess.Read));
			var signature = new string(r.ReadChars(4));
			if (signature != "FCM\x1A")
			{
				Result.Errors.Add("This is not a valid .FCM file.");
				return;
			}

			Result.Movie.HeaderEntries[HeaderKeys.Platform] = VSystemID.Raw.NES;

			var syncSettings = new NES.NESSyncSettings();

			var controllerSettings = new NESControlSettings
			{
				NesLeftPort = nameof(ControllerNES),
				NesRightPort = nameof(ControllerNES)
			};
			_deck = controllerSettings.Instantiate((x, y) => true).AddSystemToControllerDef();
			_deck.ControllerDef.BuildMnemonicsCache(Result.Movie.SystemID);

			// 004 4-byte little-endian unsigned int: version number, must be 2
			uint version = r.ReadUInt32();
			if (version != 2)
			{
				Result.Errors.Add(".FCM movie version must always be 2.");
				return;
			}

			Result.Movie.Comments.Add($"{MovieOrigin} .FCM version {version}");

			// 008 1-byte flags
			byte flags = r.ReadByte();

			/*
			 * bit 0: reserved, set to 0
			 * bit 1:
			 * if "0", movie begins from an embedded "quicksave" snapshot
			 * if "1", movie begins from reset or power-on[1]
			*/
			if (((flags >> 1) & 0x1) == 0)
			{
				Result.Errors.Add("Movies that begin with a savestate are not supported.");
				return;
			}

			/*
			 bit 2:
			 * if "0", NTSC timing
			 * if "1", "PAL" timing
			 Starting with version 0.98.12 released on September 19, 2004, a "PAL" flag was added to the header but
			 unfortunately it is not reliable - the emulator does not take the "PAL" setting from the ROM, but from a user
			 preference. This means that this site cannot calculate movie lengths reliably.
			*/
			bool pal = ((flags >> 2) & 0x1) != 0;
			Result.Movie.HeaderEntries[HeaderKeys.Pal] = pal.ToString();

			// other: reserved, set to 0
			bool syncHack = ((flags >> 4) & 0x1) != 0;
			Result.Movie.Comments.Add($"SyncHack {syncHack}");

			// 009 1-byte flags: reserved, set to 0
			r.ReadByte();

			// 00A 1-byte flags: reserved, set to 0
			r.ReadByte();

			// 00B 1-byte flags: reserved, set to 0
			r.ReadByte();

			// 00C 4-byte little-endian unsigned int: number of frames
			uint frameCount = r.ReadUInt32();

			// 010 4-byte little-endian unsigned int: rerecord count
			uint rerecordCount = r.ReadUInt32();
			Result.Movie.Rerecords = rerecordCount;
			/*
			 018 4-byte little-endian unsigned int: offset to the savestate inside file
			 The savestate offset is <header_size + length_of_metadata_in_bytes + padding>. The savestate offset should be
			 4-byte aligned. At the savestate offset there is a savestate file. The savestate exists even if the movie is
			 reset-based.
			*/
			r.ReadUInt32();

			// 01C 4-byte little-endian unsigned int: offset to the controller data inside file
			uint firstFrameOffset = r.ReadUInt32();

			// 020 16-byte md5sum of the ROM used
			byte[] md5 = r.ReadBytes(16);
			Result.Movie.HeaderEntries[HeaderKeys.Md5] = md5.BytesToHexString().ToLowerInvariant();

			// 030 4-byte little-endian unsigned int: version of the emulator used
			uint emuVersion = r.ReadUInt32();
			Result.Movie.Comments.Add($"{EmulationOrigin} FCEU {emuVersion}");

			// 034 name of the ROM used - UTF8 encoded nul-terminated string.
			var gameBytes = new List<byte>();
			while (r.PeekChar() != 0)
			{
				gameBytes.Add(r.ReadByte());
			}

			// Advance past null byte.
			r.ReadByte();
			string gameName = Encoding.UTF8.GetString(gameBytes.ToArray());
			Result.Movie.HeaderEntries[HeaderKeys.GameName] = gameName;

			/*
			 After the header comes "metadata", which is UTF8-coded movie title string. The metadata begins after the ROM
			 name and ends at the savestate offset. This string is displayed as "Author Info" in the Windows version of the
			 emulator.
			*/
			var authorBytes = new List<byte>();
			while (r.PeekChar() != 0)
			{
				authorBytes.Add(r.ReadByte());
			}

			// Advance past null byte.
			r.ReadByte();
			string author = Encoding.UTF8.GetString(authorBytes.ToArray());
			Result.Movie.HeaderEntries[HeaderKeys.Author] = author;

			// Advance to first byte of input data.
			r.BaseStream.Position = firstFrameOffset;

			SimpleController controllers = new(_deck.ControllerDef);

			string[] buttons = { "A", "B", "Select", "Start", "Up", "Down", "Left", "Right" };
			bool fds = false;

			int frame = 1;
			while (frame <= frameCount)
			{
				byte update = r.ReadByte();

				// aa: Number of delta bytes to follow
				int delta = (update >> 5) & 0x3;
				int frames = 0;

				/*
				 The delta byte(s) indicate the number of emulator frames between this update and the next update. It is
				 encoded in little-endian format and its size depends on the magnitude of the delta:
				 Delta of:	  Number of bytes:
				 0			  0
				 1-255		  1
				 256-65535	  2
				 65536-(2^24-1) 3
				*/
				for (int b = 0; b < delta; b++)
				{
					frames += r.ReadByte() * (int)Math.Pow(2, b * 8);
				}

				frame += frames;
				while (frames > 0)
				{
					Result.Movie.AppendFrame(controllers);
					if (controllers["Reset"])
					{
						controllers["Reset"] = false;
					}

					frames--;
				}

				if (((update >> 7) & 0x1) != 0)
				{
					// Control update: 0x1aabbbbb
					bool reset = false;
					int command = update & 0x1F;

					// 0xbbbbb:
					controllers["Reset"] = command == 1;
					switch (command)
					{
						case 0: // Do nothing
							break;
						case 1: // Reset
							reset = true;
							break;
						case 2: // Power cycle
							reset = true;
							if (frame != 1)
							{
								controllers["Power"] = true;
							}

							break;
						case 7: // VS System Insert Coin
							Result.Warnings.Add($"Unsupported command: VS System Insert Coin at frame {frame}");
							break;
						case 8: // VS System Dipswitch 0 Toggle
							Result.Warnings.Add($"Unsupported command: VS System Dipswitch 0 Toggle at frame {frame}");
							break;
						case 24: // FDS Insert
							fds = true;
							Result.Warnings.Add($"Unsupported command: FDS Insert at frame {frame}");
							break;
						case 25: // FDS Eject
							fds = true;
							Result.Warnings.Add($"Unsupported command: FDS Eject at frame {frame}");
							break;
						case 26: // FDS Select Side
							fds = true;
							Result.Warnings.Add($"Unsupported command: FDS Select Side at frame {frame}");
							break;
						default:
							Result.Warnings.Add($"Unknown command: {command} detected at frame {frame}");
							break;
					}

					/*
					 1 Even if the header says "movie begins from reset", the file still contains a quicksave, and the
					 quicksave is actually loaded. This flag can't therefore be trusted. To check if the movie actually
					 begins from reset, one must analyze the controller data and see if the first non-idle command in the
					 file is a Reset or Power Cycle type control command.
					*/
					if (!reset && frame == 1)
					{
						Result.Errors.Add("Movies that begin with a savestate are not supported.");
						return;
					}
				}
				else
				{
					/*
					 Controller update: 0aabbccc
					 * bb: Gamepad number minus one (?)
					*/
					int player = ((update >> 3) & 0x3) + 1;
					if (player > 2)
					{
						Result.Errors.Add("Four score not yet supported.");
						return;
					}

					/*
					 ccc:
					 * 0	  A
					 * 1	  B
					 * 2	  Select
					 * 3	  Start
					 * 4	  Up
					 * 5	  Down
					 * 6	  Left
					 * 7	  Right
					*/
					int button = update & 0x7;

					/*
					 The controller update toggles the affected input. Controller update data is emitted to the movie file
					 only when the state of the controller changes.
					*/
					controllers[$"P{player} {buttons[button]}"] = !controllers[$"P{player} {buttons[button]}"];
				}

				Result.Movie.AppendFrame(controllers);
			}

			if (fds)
			{
				Result.Movie.HeaderEntries[HeaderKeys.BoardName] = "FDS";
			}

			syncSettings.Controls = controllerSettings;
			Result.Movie.SyncSettingsJson = ConfigService.SaveWithType(syncSettings);
		}
	}
}
