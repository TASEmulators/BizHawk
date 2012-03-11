using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

#pragma warning disable 219

namespace BizHawk.MultiClient
{
	public static class MovieImport
	{
		public const string COMMENT = "comment";
		public const string EMULATIONORIGIN = "emuOrigin";
		public const string MOVIEORIGIN = "MovieOrigin";
		public const string SYNCHACK = "SyncHack";

		// Attempt to import another type of movie file into a movie object.
		public static Movie ImportFile(string path, out string errorMsg, out string warningMsg)
		{
			Movie m = new Movie();
			errorMsg = "";
			warningMsg = "";
			try
			{
				switch (Path.GetExtension(path).ToUpper())
				{
					case ".FCM":
						m = ImportFCM(path, out errorMsg, out warningMsg);
						break;
					case ".FM2":
						m = ImportFM2(path, out errorMsg, out warningMsg);
						break;
					case ".FMV":
						m = ImportFMV(path, out errorMsg, out warningMsg);
						break;
					case ".GMV":
						m = ImportGMV(path, out errorMsg, out warningMsg);
						break;
					case ".NMV":
						m = ImportNMV(path, out errorMsg, out warningMsg);
						break;
					case ".MCM":
						m = ImportMCM(path, out errorMsg, out warningMsg);
						break;
					case ".MC2":
						m = ImportMC2(path, out errorMsg, out warningMsg);
						break;
					case ".MMV":
						m = ImportMMV(path, out errorMsg, out warningMsg);
						break;
					case ".SMV":
						m = ImportSMV(path, out errorMsg, out warningMsg);
						break;
					case ".VBM":
						m = ImportVBM(path, out errorMsg, out warningMsg);
						break;
					case ".VMV":
						m = ImportVMV(path, out errorMsg, out warningMsg);
						break;
				}
				if (errorMsg == "")
				{
					m.Header.SetHeaderLine(MovieHeader.MOVIEVERSION, MovieHeader.MovieVersion);
					m.WriteMovie();
				}
			}
			catch (Exception except)
			{
				errorMsg = except.ToString();
			}
			return m;
		}

		// Return whether or not the type of file provided can currently be imported.
		public static bool IsValidMovieExtension(string extension)
		{
			string[] extensions = new string[11] {
				"FCM", "FM2", "FMV", "GMV", "NMV", "MCM", "MC2", "MMV", "SMV", "VBM", "VMV"
			};
			foreach (string ext in extensions)
				if (extension.ToUpper() == "." + ext)
					return true;
			return false;
		}

		// Import a text-based movie format. This works for .FM2 and .MC2.
		private static Movie ImportText(string path, out string errorMsg, out string warningMsg)
		{
			errorMsg = "";
			warningMsg = "";
			Movie m = new Movie(Path.ChangeExtension(path, ".tas"), MOVIEMODE.PLAY);
			FileInfo file = new FileInfo(path);
			StreamReader sr = file.OpenText();
			string[] buttons = new string[] { };
			string controller = "";
			string emulator = "";
			string platform = "";
			switch (Path.GetExtension(path).ToUpper())
			{
				case ".FM2":
					buttons = new string[8] { "Right", "Left", "Down", "Up", "Start", "Select", "B", "A" };
					controller = "NES Controller";
					emulator = "FCEUX";
					platform = "NES";
					break;
				case ".MC2":
					buttons = new string[8] { "Up", "Down", "Left", "Right", "B1", "B2", "Run", "Select" };
					controller = "PC Engine Controller";
					emulator = "Mednafen/PCEjin";
					platform = "PCE";
					break;
			}
			m.Header.SetHeaderLine(MovieHeader.PLATFORM, platform);
			SimpleController controllers = new SimpleController();
			controllers.Type = new ControllerDefinition();
			controllers.Type.Name = controller;
			int lineNum = 0;
			string line = "";
			while ((line = sr.ReadLine()) != null)
			{
				lineNum++;
				if (line == "")
					continue;
				if (line.StartsWith("emuVersion"))
					m.Header.Comments.Add(EMULATIONORIGIN + " " + emulator + " version " + ParseHeader(line, "emuVersion"));
				else if (line.StartsWith("version"))
					m.Header.Comments.Add(
						MOVIEORIGIN + " " + Path.GetExtension(path) + " version " + ParseHeader(line, "version")
					);
				else if (line.StartsWith("romFilename"))
					m.Header.SetHeaderLine(MovieHeader.GAMENAME, ParseHeader(line, "romFilename"));
				else if (line.StartsWith("romChecksum"))
				{
					string md5_blob = ParseHeader(line, "romChecksum").Trim();
					byte[] MD5 = DecodeBlob(md5_blob);
					if (MD5 != null && MD5.Length == 16)
						m.Header.SetHeaderLine("MD5", BizHawk.Util.BytesToHexString(MD5).ToLower());
					// else
					//     TODO: We should give some warning, but setting warningMsg might affect input parsing...
				}
				else if (line.StartsWith("comment author"))
					m.Header.SetHeaderLine(MovieHeader.AUTHOR, ParseHeader(line, "comment author"));
				else if (line.StartsWith("rerecordCount"))
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
					m.SetRerecords(rerecordCount);
				}
				else if (line.StartsWith("guid"))
					m.Header.SetHeaderLine(MovieHeader.GUID, ParseHeader(line, "GUID"));
				else if (line.StartsWith("StartsFromSavestate"))
				{
					// If this movie starts from a savestate, we can't support it.
					if (ParseHeader(line, "StartsFromSavestate") == "1")
					{
						errorMsg = "Movies that begin with a savestate are not supported.";
						sr.Close();
						return null;
					}
				}
				else if (line.StartsWith("palFlag"))
				{
					bool pal;
					// Try to parse the PAL setting as a bool, defaulting to false if it fails.
					try
					{
						pal = Convert.ToBoolean(int.Parse(ParseHeader(line, "palFlag")));
					}
					catch
					{
						pal = false;
					}
					m.Header.SetHeaderLine("PAL", pal.ToString());
				}
				else if (line.StartsWith("fourscore"))
					m.Header.SetHeaderLine(
						MovieHeader.FOURSCORE,
						Convert.ToBoolean(
							int.Parse(ParseHeader(line, "fourscore"))
						).ToString()
					);
				else if (line.StartsWith("sub"))
				{
					Subtitle s = new Subtitle();
					// The header name, frame, and message are separated by a space.
					int first = line.IndexOf(' ');
					int second = line.IndexOf(' ', first + 1);
					if (first != -1 && second != -1)
					{
						// Concatenate the frame and message with default values for the additional fields.
						string frame = line.Substring(first + 1, second - first - 1);
						string message = line.Substring(second + 1, line.Length - second - 1);
						m.Subtitles.AddSubtitle("subtitle " + frame + " 0 0 200 FFFFFFFF " + message);
					}
				}
				else if (line[0] == '|')
				{
					// Handle a frame of input.
					ArrayList frame = new ArrayList();
					// Split up the sections of the frame.
					string[] sections = line.Split('|');
					// Get the first invalid command warning message that arises.
					if (Path.GetExtension(path).ToUpper() == ".FM2" && warningMsg == "" && sections[1].Length != 0)
					{
						switch (sections[1][0])
						{
							case '0':
								break;
							case '1':
								controllers["Reset"] = true;
								break;
							case '2':
								if (m.Length() != 0)
								{
									warningMsg = "hard reset";
								}
								break;
							case '4':
								warningMsg = "FDS Insert";
								break;
							case '8':
								warningMsg = "FDS Select Side";
								break;
							default:
								warningMsg = "unknown";
								break;
						}
						if (warningMsg != "")
							warningMsg = "Unable to import " + warningMsg + " command on line " + lineNum + ".";
					}
					/*
					 Skip the first two sections of the split, which consist of everything before the starting | and the
					 command. Do not use the section after the last |. In other words, get the sections for the players.
					*/
					for (int section = 2; section < sections.Length - 1; section++)
					{
						// The player number is one less than the section number for the reasons explained above.
						int player = section - 1;
						// Only count lines with that have the right number of buttons and are for valid players.
						if (
							sections[section].Length == buttons.Length &&
							player <= Global.PLAYERS[controllers.Type.Name]
						)
							for (int button = 0; button < buttons.Length; button++)
								// Consider the button pressed so long as its spot is not occupied by a ".".
								controllers["P" + (player).ToString() + " " + buttons[button]] = (
									sections[section][button] != '.'
								);
					}
					// Convert the data for the controllers to a mnemonic and add it as a frame.
					MnemonicsGenerator mg = new MnemonicsGenerator();
					mg.SetSource(controllers);
					m.AppendFrame(mg.GetControllersAsMnemonic());
				}
				else
					// Everything not explicitly defined is treated as a comment.
					m.Header.Comments.Add(line);
			}
			sr.Close();
			return m;
		}

		// Get the content for a particular header.
		private static string ParseHeader(string line, string headerName)
		{
			string str;
			int x = line.LastIndexOf(headerName) + headerName.Length;
			str = line.Substring(x + 1, line.Length - x - 1);
			return str;
		}

		// Decode a blob used in FM2 (base64:..., 0x123456...)
		private static byte[] DecodeBlob(string blob)
		{
			if (blob.Length < 2) return null;
			if (blob[0] == '0' && (blob[1] == 'x' || blob[1] == 'X')) // hex
			{
				return BizHawk.Util.HexStringToBytes(blob.Substring(2));
			}
			else{ // base64
				if(!blob.StartsWith("base64:")) return null;
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

		// Remove the NULL characters from a string.
		private static string RemoveNull(string original)
		{
			string translated = "";
			for (int character = 0; character < original.Length; character++)
				if (original[character] != '\0')
					translated += original[character];
			return translated;
		}

		// FCM file format: http://code.google.com/p/fceu/wiki/FCM
		private static Movie ImportFCM(string path, out string errorMsg, out string warningMsg)
		{
			errorMsg = "";
			warningMsg = "";
			Movie m = new Movie(Path.ChangeExtension(path, ".tas"), MOVIEMODE.PLAY);
			FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
			BinaryReader r = new BinaryReader(fs);
			// 000 4-byte signature: 46 43 4D 1A "FCM\x1A"
			string signature = r.ReadStringFixedAscii(4);
			if (signature != "FCM\x1A")
			{
				errorMsg = "This is not a valid .FCM file.";
				r.Close();
				fs.Close();
				return null;
			}
			// 004 4-byte little-endian unsigned int: version number, must be 2
			uint version = r.ReadUInt32();
			if (version != 2)
			{
				errorMsg = ".FCM movie version must always be 2.";
				r.Close();
				fs.Close();
				return null;
			}
			m.Header.Comments.Add(MOVIEORIGIN + " .FCM version " + version);
			// 008 1-byte flags
			byte flags = r.ReadByte();
			// bit 0: reserved, set to 0
			/*
			 bit 1:
			 * if "0", movie begins from an embedded "quicksave" snapshot
			 * if "1", movie begins from reset or power-on[1]
			*/
			if (((flags >> 1) & 1) == 0)
			{
				errorMsg = "Movies that begin with a savestate are not supported.";
				r.Close();
				fs.Close();
				return null;
			}
			/*
			 bit 2:
			 * if "0", NTSC timing
			 * if "1", PAL timing
			 Starting with version 0.98.12 released on September 19, 2004, a PAL flag was added to the header but
			 unfortunately it is not reliable - the emulator does not take the PAL setting from the ROM, but from a user
			 preference. This means that this site cannot calculate movie lengths reliably.
			*/
			bool pal = (((flags >> 2) & 1) == 1);
			m.Header.SetHeaderLine("PAL", pal.ToString());
			// other: reserved, set to 0
			bool syncHack = (((flags >> 4) & 1) == 1);
			m.Header.Comments.Add(SYNCHACK + " " + syncHack.ToString());
			/*
			 009 1-byte flags: reserved, set to 0
			 00A 1-byte flags: reserved, set to 0
			 00B 1-byte flags: reserved, set to 0
			*/
			r.ReadBytes(3);
			// 00C 4-byte little-endian unsigned int: number of frames
			uint frameCount = r.ReadUInt32();
			// 010 4-byte little-endian unsigned int: rerecord count
			uint rerecordCount = r.ReadUInt32();
			m.SetRerecords((int)rerecordCount);
			// 014 4-byte little-endian unsigned int: length of controller data in bytes
			uint movieDataSize = r.ReadUInt32();
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
			byte[] MD5 = r.ReadBytes(16);
			m.Header.SetHeaderLine("MD5", BizHawk.Util.BytesToHexString(MD5).ToLower());
			// 030 4-byte little-endian unsigned int: version of the emulator used
			uint emuVersion = r.ReadUInt32();
			m.Header.Comments.Add(EMULATIONORIGIN + " FCEU " + emuVersion.ToString());
			// 034 name of the ROM used - UTF8 encoded nul-terminated string.
			List<byte> gameBytes = new List<byte>();
			while (r.PeekChar() != 0)
				gameBytes.Add(r.ReadByte());
			// Advance past null byte.
			r.ReadByte();
			string gameName = System.Text.Encoding.UTF8.GetString(gameBytes.ToArray());
			m.Header.SetHeaderLine(MovieHeader.GAMENAME, gameName);
			/*
			 After the header comes "metadata", which is UTF8-coded movie title string. The metadata begins after the ROM
			 name and ends at the savestate offset. This string is displayed as "Author Info" in the Windows version of the
			 emulator.
			*/
			List<byte> authorBytes = new List<byte>();
			while (r.PeekChar() != 0)
				authorBytes.Add(r.ReadByte());
			// Advance past null byte.
			r.ReadByte();
			string author = System.Text.Encoding.UTF8.GetString(authorBytes.ToArray());
			m.Header.SetHeaderLine(MovieHeader.AUTHOR, author);
			// Advance to first byte of input data.
			r.BaseStream.Position = firstFrameOffset;
			SimpleController controllers = new SimpleController();
			controllers.Type = new ControllerDefinition();
			controllers.Type.Name = "NES Controller";
			string[] buttons = new string[8] { "A", "B", "Select", "Start", "Up", "Down", "Left", "Right" };
			bool fds = false;
			bool fourscore = false;
			int frame = 1;
			while (frame <= frameCount)
			{
				MnemonicsGenerator mg = new MnemonicsGenerator();
				mg.SetSource(controllers);
				string mnemonic = mg.GetControllersAsMnemonic();
				byte update = r.ReadByte();
				// aa: Number of delta bytes to follow
				int delta = (update >> 5) & 3;
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
					frames += r.ReadByte() * (int)Math.Pow(2, b * 8);
				while (frames > 0)
				{
					m.AppendFrame(mnemonic);
					if (controllers["Reset"])
					{
						controllers["Reset"] = false;
						mnemonic = mg.GetControllersAsMnemonic();
					}
					frame++;
					frames--;
				}
				if (((update >> 7) & 1) == 1)
				{
					// Control update: 1aabbbbb
					bool reset = false;
					int command = update & 0x1F;
					// bbbbb:
					if (warningMsg == "")
					{
						switch (command)
						{
							// Do nothing
							case 0:
								break;
							// Reset
							case 1:
								reset = controllers["Reset"] = true;
								break;
							// Power cycle
							case 2:
								reset = true;
								if (frame != 1)
									warningMsg = "hard reset";
								break;
							// VS System Insert Coin
							case 7:
								warningMsg = "VS System Insert Coin";
								break;
							// VS System Dipswitch 0 Toggle
							case 8:
								warningMsg = "VS System Dipswitch 0 Toggle";
								break;
							// FDS Insert
							case 24:
								fds = true;
								warningMsg = "FDS Insert";
								break;
							// FDS Eject
							case 25:
								fds = true;
								warningMsg = "FDS Eject";
								break;
							// FDS Select Side
							case 26:
								fds = true;
								warningMsg = "FDS Select Side";
								break;
							default:
								warningMsg = "unknown";
								break;
						}
						if (warningMsg != "")
							warningMsg = "Unable to import " + warningMsg + " command at frame " + frame + ".";
					}
					/*
					 1 Even if the header says "movie begins from reset", the file still contains a quicksave, and the
					 quicksave is actually loaded. This flag can't therefore be trusted. To check if the movie actually
					 begins from reset, one must analyze the controller data and see if the first non-idle command in the
					 file is a Reset or Power Cycle type control command.
					*/
					if (!reset && frame == 1)
					{
						errorMsg = "Movies that begin with a savestate are not supported.";
						r.Close();
						fs.Close();
						return null;
					}
				}
				else
				{
					/*
					 Controller update: 0aabbccc
					 * bb: Gamepad number minus one (?)
					*/
					int player = ((update >> 3) & 3) + 1;
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
					int button = update & 7;
					/*
					 The controller update toggles the affected input. Controller update data is emitted to the movie file
					 only when the state of the controller changes.
					*/
					controllers["P" + player + " " + buttons[button]] = !controllers["P" + player + " " + buttons[button]];
				}
			}
			m.Header.SetHeaderLine(MovieHeader.PLATFORM, fds ? "FDS" : "NES");
			m.Header.SetHeaderLine(MovieHeader.FOURSCORE, fourscore.ToString());
			r.Close();
			fs.Close();
			return m;
		}

		// FM2 file format: http://www.fceux.com/web/FM2.html
		private static Movie ImportFM2(string path, out string errorMsg, out string warningMsg)
		{
			errorMsg = "";
			warningMsg = "";
			Movie m = ImportText(path, out errorMsg, out warningMsg);
			return m;
		}

		// FMV file format: http://tasvideos.org/FMV.html
		private static Movie ImportFMV(string path, out string errorMsg, out string warningMsg)
		{
			errorMsg = "";
			warningMsg = "";
			Movie m = new Movie(Path.ChangeExtension(path, ".tas"), MOVIEMODE.PLAY);
			FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
			BinaryReader r = new BinaryReader(fs);
			// 000 4-byte signature: 46 4D 56 1A "FMV\x1A"
			string signature = r.ReadStringFixedAscii(4);
			if (signature != "FMV\x1A")
			{
				errorMsg = "This is not a valid .FMV file.";
				r.Close();
				fs.Close();
				return null;
			}
			// 004 1-byte flags:
			byte flags = r.ReadByte();
			// bit 7: 0=reset-based, 1=savestate-based
			if (((flags >> 2) & 1) == 1)
			{
				errorMsg = "Movies that begin with a savestate are not supported.";
				r.Close();
				fs.Close();
				return null;
			}
			// other bits: unknown, set to 0
			// 005 1-byte flags:
			flags = r.ReadByte();
			// bit 5: is a FDS recording
			bool FDS;
			if (((flags >> 5) & 1) == 1)
			{
				FDS = true;
				m.Header.SetHeaderLine(MovieHeader.PLATFORM, "FDS");
			}
			else
			{
				FDS = false;
				m.Header.SetHeaderLine(MovieHeader.PLATFORM, "NES");
			}
			// bit 6: uses controller 2
			bool controller2 = (((flags >> 6) & 1) == 1);
			// bit 7: uses controller 1
			bool controller1 = (((flags >> 7) & 1) == 1);
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
			m.SetRerecords(((int)rerecordCount) + 1);
			// 00E 2-byte little-endian unsigned int: unknown, set to 0000
			r.ReadInt16();
			// 010 64-byte zero-terminated emulator identifier string
			string emuVersion = RemoveNull(r.ReadStringFixedAscii(64));
			m.Header.Comments.Add(EMULATIONORIGIN + " Famtasia version " + emuVersion);
			// 050 64-byte zero-terminated movie title string
			string description = RemoveNull(r.ReadStringFixedAscii(64));
			m.Header.Comments.Add(COMMENT + " " + description);
			if (!controller1 && !controller2 && !FDS)
			{
				warningMsg = "No input recorded.";
				r.Close();
				fs.Close();
				return m;
			}
			/*
			 The file format has no means of identifying NTSC/PAL. It is always assumed that the game is NTSC - that is, 60
			 fps.
			*/
			m.Header.SetHeaderLine("PAL", "False");
			// 090 frame data begins here
			SimpleController controllers = new SimpleController();
			controllers.Type = new ControllerDefinition();
			controllers.Type.Name = "NES Controller";
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
			string[] buttons = new string[8] { "Right", "Left", "Up", "Down", "B", "A", "Select", "Start" };
			/*
			 The file has no terminator byte or frame count. The number of frames is the <filesize minus 144> divided by
			 <number of bytes per frame>.
			*/
			int bytesPerFrame = 0;
			if (controller1)
				bytesPerFrame++;
			if (controller2)
				bytesPerFrame++;
			if (FDS)
				bytesPerFrame++;
			long frames = (fs.Length - 144) / bytesPerFrame;
			for (long frame = 1; frame <= frames; frame++)
			{
				/*
				 Each frame consists of 1 or more bytes. Controller 1 takes 1 byte, controller 2 takes 1 byte, and the FDS
				 data takes 1 byte. If all three exist, the frame is 3 bytes. For example, if the movie is a regular NES game
				 with only controller 1 data, a frame is 1 byte.
				*/
				int player = 1;
				while (player <= 3)
				{
					if (player == 1 && !controller1)
						player++;
					if (player == 2 && !controller2)
						player++;
					if (player != 3)
					{
						byte controllerState = r.ReadByte();
						for (int button = 0; button < buttons.Length; button++)
							controllers["P" + player + " " + buttons[button]] = (((controllerState >> button) & 1) == 1);
					}
					else if (FDS)
					{
						// TODO: FDS data handling here.
						byte command = r.ReadByte();
					}
					player++;
				}
				MnemonicsGenerator mg = new MnemonicsGenerator();
				mg.SetSource(controllers);
				m.AppendFrame(mg.GetControllersAsMnemonic());
			}
			r.Close();
			fs.Close();
			return m;
		}

		// GMV file format: http://code.google.com/p/gens-rerecording/wiki/GMV
		private static Movie ImportGMV(string path, out string errorMsg, out string warningMsg)
		{
			errorMsg = "";
			warningMsg = "";
			Movie m = new Movie(Path.ChangeExtension(path, ".tas"), MOVIEMODE.PLAY);
			FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
			BinaryReader r = new BinaryReader(fs);
			// 000 16-byte signature and format version: "Gens Movie TEST9"
			string signature = r.ReadStringFixedAscii(15);
			if (signature != "Gens Movie TEST")
			{
				errorMsg = "This is not a valid .GMV file.";
				r.Close();
				fs.Close();
				return null;
			}
			// 00F ASCII-encoded GMV file format version. The most recent is 'A'. (?)
			string version = r.ReadStringFixedAscii(1);
			m.Header.Comments.Add(MOVIEORIGIN + " .GMV version " + version);
			// 010 4-byte little-endian unsigned int: rerecord count
			uint rerecordCount = r.ReadUInt32();
			m.SetRerecords((int)rerecordCount);
			// 014 ASCII-encoded controller config for player 1. '3' or '6'.
			string player1Config = r.ReadStringFixedAscii(1);
			// 015 ASCII-encoded controller config for player 2. '3' or '6'.
			string player2Config = r.ReadStringFixedAscii(1);
			if (player1Config == "6" || player2Config == "6")
				warningMsg = "6 button controllers are not properly supported.";
			// 016 special flags (Version A and up only)
			byte flags = r.ReadByte();
			/*
			 bit 7 (most significant): if "1", movie runs at 50 frames per second; if "0", movie runs at 60 frames per second
			 The file format has no means of identifying NTSC/PAL, but the FPS can still be derived from the header.
			*/
			bool pal = (((flags >> 7) & 1) == 1);
			m.Header.SetHeaderLine("PAL", pal.ToString());
			// bit 6: if "1", movie requires a savestate.
			if (((flags >> 6) & 1) == 1)
			{
				errorMsg = "Movies that begin with a savestate are not supported.";
				r.Close();
				fs.Close();
				return null;
			}
			// bit 5: if "1", movie is 3-player movie; if "0", movie is 2-player movie
			bool threePlayers = (((flags >> 5) & 1) == 1);
			// Unknown.
			r.ReadByte();
			// 018 40-byte zero-terminated ASCII movie name string
			string description = RemoveNull(r.ReadStringFixedAscii(40));
			m.Header.Comments.Add(COMMENT + " " + description);
			SimpleController controllers = new SimpleController();
			controllers.Type = new ControllerDefinition();
			controllers.Type.Name = "Genesis 3-Button Controller";
			/*
			 040 frame data
			 For controller bytes, each value is determined by OR-ing together values for whichever of the following are left
			 unpressed:
			 * 0x01 Up
			 * 0x02 Down
			 * 0x04 Left
			 * 0x08 Right
			 * 0x10 A
			 * 0x20 B
			 * 0x40 C
			 * 0x80 Start
			*/
			string[] buttons = new string[8] { "Up", "Down", "Left", "Right", "A", "B", "C", "Start" };
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
			string[] other = new string[4] { "X", "Y", "Z", "Mode" };
			// The file has no terminator byte or frame count. The number of frames is the <filesize minus 64> divided by 3.
			long frames = (fs.Length - 64) / 3;
			for (long frame = 1; frame <= frames; frame++)
			{
				// Each frame consists of 3 bytes.
				for (int player = 1; player <= 3; player++)
				{
					byte controllerState = r.ReadByte();
					// * is controller 3 if a 3-player movie, or XYZ-mode if a 2-player movie.
					if (player != 3 || threePlayers)
						for (int button = 0; button < buttons.Length; button++)
							controllers["P" + player + " " + buttons[button]] = (((controllerState >> button) & 1) == 0);
					else
						for (int button = 0; button < other.Length; button++)
						{
							if (player1Config == "6")
								controllers["P1 " + other[button]] = (((controllerState >> button) & 1) == 0);
							if (player2Config == "6")
								controllers["P2 " + other[button]] = (((controllerState >> (button + 4)) & 1) == 0);
						}
				}
				MnemonicsGenerator mg = new MnemonicsGenerator();
				mg.SetSource(controllers);
				m.AppendFrame(mg.GetControllersAsMnemonic());
			}
			return m;
		}

		// MCM file format: http://code.google.com/p/mednafen-rr/wiki/MCM
		//   Mednafen-rr switched to MC2 from r261, so see r260 for details.
		private static Movie ImportMCM(string path, out string errorMsg, out string warningMsg)
		{
			const string SIGNATURE = "MDFNMOVI";
			const int MOVIE_VERSION = 1;
			const string CONSOLE_PCE = "pce";
			const string CTRL_TYPE_PCE = "PC Engine Controller";
			string[] CTRL_BUTTONS = {
				"B1", "B2", "Select", "Run", "Up", "Right", "Down", "Left"
			};
			const int INPUT_SIZE_PCE = 11;
			const int INPUT_PORT_COUNT = 5;

			errorMsg = "";
			warningMsg = "";
			Movie m = new Movie(Path.ChangeExtension(path, ".tas"), MOVIEMODE.PLAY);
			bool success = false;

			// Unless otherwise noted, all values are little-endian.
			FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
			BinaryReader r = new BinaryReader(fs);
			// 000 8-byte signature: 4D 44 46 4E 4D 4F 56 49 "MDFNMOVI"
			string signature = r.ReadStringFixedAscii(SIGNATURE.Length);
			if (signature != SIGNATURE)
			{
				errorMsg = "This is not a valid .MCM file.";
				goto FAIL;
			}
			// 008 uint32: emulator version, current is 0x0000080A
			uint emuVersion = r.ReadUInt32();
			m.Header.Comments.Add(EMULATIONORIGIN + " Mednafen " + emuVersion.ToString());
			// 00C uint32: movie format version, must be 1
			uint version = r.ReadUInt32();
			if (version != MOVIE_VERSION)
			{
				errorMsg = ".MCM movie version must always be 1.";
				goto FAIL;
			}
			m.Header.Comments.Add(MOVIEORIGIN + " .MCM version " + version);
			// 010 32-byte (actually, 16-byte) md5sum of the ROM used
			byte[] MD5 = r.ReadBytes(16);
			r.ReadBytes(16); // discard
			m.Header.SetHeaderLine("MD5", BizHawk.Util.BytesToHexString(MD5).ToLower());
			// 030 64-byte filename of ROM used (with extension)
			string gameName = RemoveNull(r.ReadStringFixedAscii(64));
			m.Header.SetHeaderLine(MovieHeader.GAMENAME, gameName);
			// 070 uint32: re-record count
			uint rerecordCount = r.ReadUInt32();
			m.SetRerecords((int)rerecordCount);
			// 074 5-byte console indicator (pce, ngp, pcfx, wswan)
			string console = RemoveNull(r.ReadStringFixedAscii(5));
			if (console != CONSOLE_PCE) // for now, bizhawk does not support ngp, pcfx, wswan
			{
				errorMsg = "Only PCE movies are supported.";
				goto FAIL;
			}
			string platform = "PCE";
			m.Header.SetHeaderLine(MovieHeader.PLATFORM, platform);
			// 079 32-byte author name
			string author = RemoveNull(r.ReadStringFixedAscii(32));
			m.Header.SetHeaderLine(MovieHeader.AUTHOR, author);
			// 099 103-byte padding 0s
			r.ReadBytes(103); // discard

			// 100 variable: input data
			SimpleController controllers = new SimpleController();
			controllers.Type = new ControllerDefinition();
			controllers.Type.Name = CTRL_TYPE_PCE;
			//   For PCE, BizHawk does not have any control command. So I ignore any command.
			for (byte[] input = r.ReadBytes(INPUT_SIZE_PCE);
			     input.Length == INPUT_SIZE_PCE; 
			     input = r.ReadBytes(INPUT_SIZE_PCE))
			{
				for (int player = 1; player <= INPUT_PORT_COUNT; ++player)
				{
					byte pad = input[1+2*(player-1)];
					for(int i = 0; i < 8; ++i)
					{
						if ((pad & (1 << i)) != 0)
							controllers["P" + player + " " + CTRL_BUTTONS[i]] = true;
					}
				}
				MnemonicsGenerator mg = new MnemonicsGenerator();
				mg.SetSource(controllers);
				m.AppendFrame(mg.GetControllersAsMnemonic());
			}

			success = true;
FAIL:
			if(!success) m = null;
			r.Close();
			fs.Close();
			return m;
		}

		// MC2 file format: http://code.google.com/p/pcejin/wiki/MC2
		private static Movie ImportMC2(string path, out string errorMsg, out string warningMsg)
		{
			errorMsg = "";
			warningMsg = "";
			Movie m = ImportText(path, out errorMsg, out warningMsg);
			// TODO: PCECD equivalent.
			return m;
		}

		// MMV file format: http://tasvideos.org/MMV.html
		private static Movie ImportMMV(string path, out string errorMsg, out string warningMsg)
		{
			errorMsg = "";
			warningMsg = "";
			Movie m = new Movie(Path.ChangeExtension(path, ".tas"), MOVIEMODE.PLAY);
			FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
			BinaryReader r = new BinaryReader(fs);
			// 0000: 4-byte signature: "MMV\0"
			string signature = r.ReadStringFixedAscii(4);
			if (signature != "MMV\0")
			{
				errorMsg = "This is not a valid .MMV file.";
				r.Close();
				fs.Close();
				return null;
			}
			// 0004: 4-byte little endian unsigned int: dega version
			uint emuVersion = r.ReadUInt32();
			m.Header.Comments.Add(EMULATIONORIGIN + " Dega version " + emuVersion.ToString());
			// 0008: 4-byte little endian unsigned int: frame count
			uint frameCount = r.ReadUInt32();
			// 000c: 4-byte little endian unsigned int: rerecord count
			uint rerecordCount = r.ReadUInt32();
			m.SetRerecords((int)rerecordCount);
			// 0010: 4-byte little endian flag: begin from reset?
			uint reset = r.ReadUInt32();
			if (reset == 0)
			{
				errorMsg = "Movies that begin with a savestate are not supported.";
				r.Close();
				fs.Close();
				return null;
			}
			// 0014: 4-byte little endian unsigned int: offset of state information
			r.ReadUInt32();
			// 0018: 4-byte little endian unsigned int: offset of input data
			r.ReadUInt32();
			// 001c: 4-byte little endian unsigned int: size of input packet
			r.ReadUInt32();
			// 0020-005f: string: author info (UTF-8)
			string author = RemoveNull(r.ReadStringFixedAscii(64));
			m.Header.SetHeaderLine(MovieHeader.AUTHOR, author);
			// 0060: 4-byte little endian flags
			byte flags = r.ReadByte();
			// bit 0: unused
			// bit 1: PAL
			bool pal = (((flags >> 1) & 1) == 1);
			m.Header.SetHeaderLine("PAL", pal.ToString());
			// bit 2: Japan
			bool japan = (((flags >> 2) & 1) == 1);
			m.Header.SetHeaderLine("Japan", japan.ToString());
			// bit 3: Game Gear (version 1.16+)
			bool gamegear;
			if (((flags >> 3) & 1) == 1)
			{
				gamegear = true;
				m.Header.SetHeaderLine(MovieHeader.PLATFORM, "GG");
			}
			else
			{
				gamegear = false;
				m.Header.SetHeaderLine(MovieHeader.PLATFORM, "SMS");
			}
			// bits 4-31: unused
			r.ReadBytes(3);
			// 0064-00e3: string: rom name (ASCII)
			string gameName = RemoveNull(r.ReadStringFixedAscii(128));
			m.Header.SetHeaderLine(MovieHeader.GAMENAME, gameName);
			// 00e4-00f3: binary: rom MD5 digest
			byte[] MD5 = r.ReadBytes(16);
			m.Header.SetHeaderLine("MD5", String.Format("{0:x8}", BizHawk.Util.BytesToHexString(MD5).ToLower()));
			SimpleController controllers = new SimpleController();
			controllers.Type = new ControllerDefinition();
			controllers.Type.Name = "SMS Controller";
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
			string[] buttons = new string[6] { "Up", "Down", "Left", "Right", "B1", "B2" };
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
						controllers["P" + player + " " + buttons[button]] = (((controllerState >> button) & 1) == 1);
					if (player == 1)
						controllers["Pause"] = (
							(((controllerState >> 6) & 1) == 1 && (!gamegear)) ||
							(((controllerState >> 7) & 1) == 1 && gamegear)
						);
				}
				MnemonicsGenerator mg = new MnemonicsGenerator();
				mg.SetSource(controllers);
				m.AppendFrame(mg.GetControllersAsMnemonic());
			}
			r.Close();
			fs.Close();
			return m;
		}

		// NMV file format: http://tasvideos.org/NMV.html
		private static Movie ImportNMV(string path, out string errorMsg, out string warningMsg)
		{
			errorMsg = "";
			warningMsg = "";
			Movie m = new Movie(Path.ChangeExtension(path, ".tas"), MOVIEMODE.PLAY);
			FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
			BinaryReader r = new BinaryReader(fs);
			// 000 4-byte signature: 4E 53 53 1A "NSS\x1A"
			string signature = r.ReadStringFixedAscii(4);
			if (signature != "NSS\x1A")
			{
				errorMsg = "This is not a valid .NMV file.";
				r.Close();
				fs.Close();
				return null;
			}
			r.Close();
			fs.Close();
			return m;
		}

		private static Movie ImportSMV(string path, out string errorMsg, out string warningMsg)
		{
			errorMsg = "";
			warningMsg = "";
			FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
			BinaryReader r = new BinaryReader(fs);

			byte[] signatureBytes = new byte[4];
			for (int x = 0; x < 4; x++)
				signatureBytes[x] = r.ReadByte();
			string signature = System.Text.Encoding.UTF8.GetString(signatureBytes);
			if (signature.Substring(0, 3) != "SMV")
			{
				errorMsg = "This is not a valid .SMV file.";
				r.Close();
				fs.Close();
				return null;
			}

			uint version = r.ReadUInt32();

			switch (version)
			{
				case 1:
					return ImportSMV143(r, path);
				case 4:
					return ImportSMV151(r, path);
				case 5:
					return ImportSMV152(r, path);
				default:
				{
					errorMsg = "SMV version not recognized, 143, 151, and 152 are currently supported.";
					r.Close();
					fs.Close();
					return null;
				}
			}
		}

		// SMV 1.43 file format: http://code.google.com/p/snes9x-rr/wiki/SMV143
		private static Movie ImportSMV143(BinaryReader r, string path)
		{
			Movie m = new Movie(Path.ChangeExtension(path, ".tas"), MOVIEMODE.PLAY);

			uint GUID = r.ReadUInt32();
			m.Header.SetHeaderLine(MovieHeader.GUID, GUID.ToString()); //TODO: format to hex string
			m.SetRerecords((int)r.ReadUInt32());

			uint frameCount = r.ReadUInt32();
			byte ControllerFlags = r.ReadByte();

			int numControllers;
			if (((ControllerFlags >> 4) & 1) == 1)
				numControllers = 5;
			else if (((ControllerFlags >> 3) & 1) == 1)
				numControllers = 4;
			else if (((ControllerFlags >> 2) & 1) == 1)
				numControllers = 3;
			else if (((ControllerFlags >> 1) & 1) == 1)
				numControllers = 2;
			else
				numControllers = 1;

			byte MovieFlags = r.ReadByte();

			if ((MovieFlags & 1) == 0)
				return null; //TODO: Savestate movies not supported error

			if (((MovieFlags >> 1) & 1) == 1)
				m.Header.SetHeaderLine("PAL", "True");

			byte SyncOptions = r.ReadByte();
			byte SyncOptions2 = r.ReadByte();
			//TODO: these

			uint SavestateOffset = r.ReadUInt32();
			uint FrameDataOffset = r.ReadUInt32();

			//TODO: get extra rom info

			r.BaseStream.Position = FrameDataOffset;
			for (int frame = 1; frame <= frameCount; frame++)
			{
				//TODO: FF FF for all controllers = Reset
				//string frame = "|0|";
				for (int controller = 1; controller <= numControllers; controller++)
				{
					ushort fd = r.ReadUInt16();
				}
			}
			return m;
		}

		// SMV 1.51 file format: http://code.google.com/p/snes9x-rr/wiki/SMV151
		private static Movie ImportSMV151(BinaryReader r, string path)
		{
			Movie m = new Movie(Path.ChangeExtension(path, ".tas"), MOVIEMODE.PLAY);
			return m;
		}

		private static Movie ImportSMV152(BinaryReader r, string path)
		{
			Movie m = new Movie(Path.ChangeExtension(path, ".tas"), MOVIEMODE.PLAY);
			uint GUID = r.ReadUInt32();
			return m;
		}

		//VBM file format: http://code.google.com/p/vba-rerecording/wiki/VBM
		private static Movie ImportVBM(string path, out string errorMsg, out string warningMsg)
		{
			errorMsg = "";
			warningMsg = "";
			Movie m = new Movie(Path.ChangeExtension(path, ".tas"), MOVIEMODE.PLAY);
			FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
			BinaryReader r = new BinaryReader(fs);
			// 000 4-byte signature: 56 42 4D 1A "VBM\x1A"
			string signature = r.ReadStringFixedAscii(4);
			if (signature != "VBM\x1A")
			{
				errorMsg = "This is not a valid .VBM file.";
				r.Close();
				fs.Close();
				return null;
			}
			// 004 4-byte little-endian unsigned int: major version number, must be "1"
			uint majorVersion = r.ReadUInt32();
			if (majorVersion != 1)
			{
				errorMsg = ".VBM major movie version must be 1.";
				r.Close();
				fs.Close();
				return null;
			}
			/*
			 008 4-byte little-endian integer: movie "uid" - identifies the movie-savestate relationship, also used as the
			 recording time in Unix epoch format
			*/
			uint uid = r.ReadUInt32();
			m.Header.SetHeaderLine(MovieHeader.GUID, String.Format("{0:x8}", uid) + "-0000-0000-0000-000000000000");
			// 00C 4-byte little-endian unsigned int: number of frames
			uint frameCount = r.ReadUInt32();
			// 010 4-byte little-endian unsigned int: rerecord count
			uint rerecordCount = r.ReadUInt32();
			m.SetRerecords((int)rerecordCount);
			// 014 1-byte flags: (movie start flags)
			byte flags = r.ReadByte();
			// bit 0: if "1", movie starts from an embedded "quicksave" snapshot
			bool startfromquicksave = ((flags & 1) == 1);
			// bit 1: if "1", movie starts from reset with an embedded SRAM
			bool startfromsram = (((flags >> 1) & 1) == 1);
			// other: reserved, set to 0
			// We can't start from either save option.
			if (startfromquicksave || startfromsram)
			{
				errorMsg = "Movies that begin with a save are not supported.";
				// (If both bits 0 and 1 are "1", the movie file is invalid)
				if (startfromquicksave && startfromsram)
					errorMsg = "The movie file is invalid.";
				r.Close();
				fs.Close();
				return null;
			}
			//015 1-byte flags: controller flags
			flags = r.ReadByte();
			// TODO: Handle the additional controllers.
			int players = 0;
			// bit 0: controller 1 in use
			if ((flags & 1) == 1)
				players++;
			else
			{
				errorMsg = "Controller 1 must be in use.";
				r.Close();
				fs.Close();
				return null;
			}
			// bit 1: controller 2 in use (SGB games can be 2-player multiplayer)
			if (((flags >> 1) & 1) == 1)
				players++;
			// bit 2: controller 3 in use (SGB games can be 3- or 4-player multiplayer with multitap)
			if (((flags >> 2) & 1) == 1)
				players++;
			// bit 3: controller 4 in use (SGB games can be 3- or 4-player multiplayer with multitap)
			if (((flags >> 3) & 1) == 1)
				players++;
			// other: reserved
			// 016 1-byte flags: system flags (game always runs at 60 frames/sec)
			flags = r.ReadByte();
			// bit 0: if "1", movie is for the GBA system
			bool is_gba = ((flags & 1) == 1);
			// bit 1: if "1", movie is for the GBC system
			bool is_gbc = (((flags >> 1) & 1) == 1);
			// bit 2: if "1", movie is for the SGB system
			bool is_sgb = (((flags >> 2) & 1) == 1);
			// other: reserved, set to 0
			// (At most one of bits 0, 1, 2 can be "1")
			if (!(is_gba ^ is_gbc ^ is_sgb) && (is_gba || is_gbc || is_sgb))
			{
				errorMsg = "This is not a valid .VBM file.";
				r.Close();
				fs.Close();
				return null;
			}
			// (If all 3 of these bits are "0", it is for regular GB.)
			string platform = "GB";
			if (is_gba)
				platform = "GBA";
			if (is_gbc)
				platform = "GBC";
			if (is_sgb)
				platform = "SGB";
			m.Header.SetHeaderLine(MovieHeader.PLATFORM, platform);
			// 017 1-byte flags: (values of some boolean emulator options)
			flags = r.ReadByte();
			/*
			 * bit 0: (useBiosFile) if "1" and the movie is of a GBA game, the movie was made using a GBA BIOS file.
			 * bit 1: (skipBiosFile) if "0" and the movie was made with a GBA BIOS file, the BIOS intro is included in the
			 * movie.
			 * bit 2: (rtcEnable) if "1", the emulator "real time clock" feature was enabled.
			*/
			// bit 3: (unsupported) must be "0" or the movie file is considered invalid (legacy).
			if (((flags >> 3) & 1) == 1)
			{
				errorMsg = "This is not a valid .VBM file.";
				r.Close();
				fs.Close();
				return null;
			}
			/*
			 * bit 4: (lagReduction) if "0" and the movie is of a GBA game, the movie was made using the old excessively
			 * laggy GBA timing.
			 * bit 5: (gbcHdma5Fix) if "0" and the movie is of a GBC game, the movie was made using the old buggy HDMA5
			 * timing.
			 * bit 6: (echoRAMFix)  if "1" and the movie is of a GB, GBC, or SGB game, the movie was made with Echo RAM Fix
			 * on, otherwise it was made with Echo RAM Fix off.
			 * bit 7: reserved, set to 0.
			*/
			/*
			 018 4-byte little-endian unsigned int: theApp.winSaveType (value of that emulator option)
			 01C 4-byte little-endian unsigned int: theApp.winFlashSize (value of that emulator option)
			 020 4-byte little-endian unsigned int: gbEmulatorType (value of that emulator option)
			*/
			r.ReadBytes(12);
			/*
			 024 12-byte character array: the internal game title of the ROM used while recording, not necessarily
			 null-terminated (ASCII?)
			*/
			string gameName = RemoveNull(r.ReadStringFixedAscii(12));
			m.Header.SetHeaderLine(MovieHeader.GAMENAME, gameName);
			// 030 1-byte unsigned char: minor version/revision number of current VBM version, the latest is "1"
			byte minorVersion = r.ReadByte();
			m.Header.Comments.Add(MOVIEORIGIN + " .VBM version " + majorVersion + "." + minorVersion);
			/*
			 031 1-byte unsigned char: the internal CRC of the ROM used while recording
			 032 2-byte little-endian unsigned short: the internal Checksum of the ROM used while recording, or a calculated
			 CRC16 of the BIOS if GBA
			 034 4-byte little-endian unsigned int: the Game Code of the ROM used while recording, or the Unit Code if not
			 GBA
			 038 4-byte little-endian unsigned int: offset to the savestate or SRAM inside file, set to 0 if unused
			*/
			r.ReadBytes(11);
			// 03C 4-byte little-endian unsigned int: offset to the controller data inside file
			uint firstFrameOffset = r.ReadUInt32();
			// After the header is 192 bytes of text. The first 64 of these 192 bytes are for the author's name (or names).
			string author = RemoveNull(r.ReadStringFixedAscii(64));
			m.Header.SetHeaderLine(MovieHeader.AUTHOR, author);
			// The following 128 bytes are for a description of the movie. Both parts must be null-terminated.
			string movieDescription = RemoveNull(r.ReadStringFixedAscii(128));
			m.Header.Comments.Add(COMMENT + " " + movieDescription);
			/*
			 TODO: implement start data. There are no specifics on the googlecode page as to how long the SRAM or savestate
			 should be.
			*/
			// If there is no "Start Data", this will probably begin at byte 0x100 in the file, but this is not guaranteed.
			r.BaseStream.Position = firstFrameOffset;
			SimpleController controllers = new SimpleController();
			controllers.Type = new ControllerDefinition();
			controllers.Type.Name = "Gameboy Controller";
			/*
			 * 01 00 A
			 * 02 00 B
			 * 04 00 Select
			 * 08 00 Start
			 * 10 00 Right
			 * 20 00 Left
			 * 40 00 Up
			 * 80 00 Down
			*/
			string[] buttons = new string[8] { "A", "B", "Select", "Start", "Right", "Left", "Up", "Down" };
			/*
			 * 00 01 R
			 * 00 02 L
			 * 00 04 Reset (old timing)
			 * 00 08 Reset (new timing since version 1.1)
			 * 00 10 Left motion sensor
			 * 00 20 Right motion sensor
			 * 00 40 Down motion sensor
			 * 00 80 Up motion sensor
			*/
			string[] other = new string[8] {
				"R", "L", "Reset (old timing)" , "Reset (new timing since version 1.1)", "Left motion sensor",
				"Right motion sensor", "Down motion sensor", "Up motion sensor"
			};
			for (int frame = 1; frame <= frameCount; frame++)
			{
				/*
				 A stream of 2-byte bitvectors which indicate which buttons are pressed at each point in time. They will come
				 in groups of however many controllers are active, in increasing order.
				*/
				ushort controllerState = r.ReadUInt16();
				for (int button = 0; button < buttons.Length; button++)
					controllers[buttons[button]] = (((controllerState >> button) & 1) == 1);
				MnemonicsGenerator mg = new MnemonicsGenerator();
				mg.SetSource(controllers);
				m.AppendFrame(mg.GetControllersAsMnemonic());
				// TODO: Handle the other buttons.
				if (warningMsg == "")
				{
					for (int button = 0; button < other.Length; button++)
						if (((controllerState >> (button + 8)) & 1) == 1)
						{
							warningMsg = other[button];
							break;
						}
					if (warningMsg != "")
						warningMsg = "Unable to import " + warningMsg + " at frame " + frame + ".";
				}
				// TODO: Handle the additional controllers.
				r.ReadBytes((players - 1) * 2);
			}
			r.Close();
			fs.Close();
			return m;
		}

		// VMV file format: http://tasvideos.org/VMV.html
		private static Movie ImportVMV(string path, out string errorMsg, out string warningMsg)
		{
			errorMsg = "";
			warningMsg = "";
			Movie m = new Movie(Path.ChangeExtension(path, ".tas"), MOVIEMODE.PLAY);
			FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
			BinaryReader r = new BinaryReader(fs);
			// 000 12-byte signature: "VirtuaNES MV"
			string signature = r.ReadStringFixedAscii(12);
			if (signature != "VirtuaNES MV")
			{
				errorMsg = "This is not a valid .VMV file.";
				r.Close();
				fs.Close();
				return null;
			}
			r.Close();
			fs.Close();
			return m;
		}
	}
}
