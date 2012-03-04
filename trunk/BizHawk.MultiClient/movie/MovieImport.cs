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

		/*
		Read bytes from a BinaryReader and translate them into a string for either the hexidecimal representation of the
		binary numbers or the UTF-8 string they represent.
		*/
		private static string BytesToString(BinaryReader r, int size, bool hexadecimal = false)
		{
			byte[] bytes = new byte[size];
			for (int b = 0; b < size; b++)
				bytes[b] = r.ReadByte();
			if (hexadecimal)
				return string.Concat(bytes.Select(b => string.Format("{0:x2}", b)));
			return System.Text.Encoding.UTF8.GetString(bytes);
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
			int lineNum = 0;
			string line = "";
			while ((line = sr.ReadLine()) != null)
			{
				lineNum++;
				if (line == "")
					continue;
				if (line.StartsWith("emuVersion"))
					m.Header.Comments.Add("emuOrigin " + emulator + " version " + ParseHeader(line, "emuVersion"));
				else if (line.StartsWith("version"))
					m.Header.Comments.Add(
						"MovieOrigin " + Path.GetExtension(path) + " version " + ParseHeader(line, "version")
					);
				else if (line.StartsWith("romFilename"))
					m.Header.SetHeaderLine(MovieHeader.GAMENAME, ParseHeader(line, "romFilename"));
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
					// Start building the data for the controllers.
					SimpleController controllers = new SimpleController();
					controllers.Type = new ControllerDefinition();
					controllers.Type.Name = controller;
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
			string signature = BytesToString(r, 4);
			if (signature != "FCM\x1A")
			{
				errorMsg = "This is not a valid .FCM file.";
				r.Close();
				fs.Close();
				return null;
			}
			// 004 4-byte little-endian unsigned int: version number, must be 2
			uint version = r.ReadUInt32();
			m.Header.Comments.Add("MovieOrigin .FCM version " + version);
			// 008 1-byte flags
			byte flags = r.ReadByte();
			/*
			 * bit 0: reserved, set to 0
			 * bit 1:
			 ** if "0", movie begins from an embedded "quicksave" snapshot
			 ** if "1", movie begins from reset or power-on[1]
			 * bit 2:
             ** if "0", NTSC timing
             ** if "1", PAL timing
             ** see notes below
             * other: reserved, set to 0
			*/
			if ((int)(flags & 2) == 0)
			{
				errorMsg = "Movies that begin with a savestate are not supported.";
				r.Close();
				fs.Close();
				return null;
			}
			bool pal = false;
			if ((int)(flags & 4) != 0)
				pal = true;
			m.Header.SetHeaderLine("PAL", pal.ToString());
			bool movieSyncHackOn = true;
			if ((int)(flags & 16) != 0)
				movieSyncHackOn = false;
			m.Header.SetHeaderLine("SyncHack", movieSyncHackOn.ToString());
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
			string MD5 = BytesToString(r, 16, true);
			m.Header.SetHeaderLine("MD5", MD5);
			// 030 4-byte little-endian unsigned int: version of the emulator used
			uint emuVersion = r.ReadUInt32();
			m.Header.Comments.Add("emuOrigin FCEU " + emuVersion.ToString());
			// 034 name of the ROM used - UTF8 encoded nul-terminated string.
			List<byte> gameBytes = new List<byte>();
			while (r.PeekChar() != 0)
				gameBytes.Add(r.ReadByte());
			// Advance past null byte.
			r.ReadByte();
			string gameName = System.Text.Encoding.UTF8.GetString(gameBytes.ToArray());
			m.Header.SetHeaderLine(MovieHeader.GAMENAME, gameName);
			/*
			After the header comes "metadata", which is UTF8-coded movie title string. The metadata begins after the ROM name
			and ends at the savestate offset. This string is displayed as "Author Info" in the Windows version of the
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
			string[] buttons = new string[8] { "A", "B", "Select", "Start", "Up", "Down", "Left", "Right" };
			bool fourscore = false;
			int frame = 1;
			while (frame <= frameCount)
			{
				byte update = r.ReadByte();
				SimpleController controllers = new SimpleController();
				controllers.Type = new ControllerDefinition();
				controllers.Type.Name = "NES Controller";
				if ((int)(update & 0x80) != 0)
				{
					// Control update: 1aabbbbb
					bool reset = false;
					int command = update & 0x1F;
					/*
					bbbbb:
					** 0     Do nothing
					** 1     Reset
					** 2     Power cycle
					** 7     VS System Insert Coin
					** 8     VS System Dipswitch 0 Toggle
					** 24    FDS Insert
					** 25    FDS Eject
					** 26    FDS Select Side
					*/
					switch (command)
					{
						case 0:
							break;
						case 1:
							reset = true;
							controllers["Reset"] = true;
							break;
						case 2:
							reset = true;
							if (frame != 1)
							{
								warningMsg = "hard reset";
							}
							break;
						case 7:
							warningMsg = "VS System Insert Coin";
							break;
						case 8:
							warningMsg = "VS System Dipswitch 0 Toggle";
							break;
						case 24:
							warningMsg = "FDS Insert";
							break;
						case 25:
							warningMsg = "FDS Eject";
							break;
						case 26:
							warningMsg = "FDS Select Side";
							break;
						default:
							warningMsg = "unknown";
							break;
					}
					if (warningMsg != "")
						warningMsg = "Unable to import " + warningMsg + " command at frame " + frame + ".";
					/*
					1 Even if the header says "movie begins from reset", the file still contains a quicksave, and the
					quicksave is actually loaded. This flag can't therefore be trusted. To check if the movie actually begins
					from reset, one must analyze the controller data and see if the first non-idle command in the file is a
					Reset or Power Cycle type control command.
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
					 * 0      A
					 * 1      B
					 * 2      Select
					 * 3      Start
					 * 4      Up
					 * 5      Down
					 * 6      Left
					 * 7      Right
					*/
					int button = update & 7;
					controllers["P" + player + " " + buttons[button]] = !controllers["P" + player + " " + buttons[button]];
				}
				// aa: Number of delta bytes to follow
				int delta = (update >> 5) & 3;
				r.ReadBytes(delta);
				MnemonicsGenerator mg = new MnemonicsGenerator();
				mg.SetSource(controllers);
				string mnemonic = mg.GetControllersAsMnemonic();
				m.AppendFrame(mnemonic);
				frame++;
			}
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
			string signature = BytesToString(r, 4);
			if (signature != "FMV\x1A")
			{
				errorMsg = "This is not a valid .FMV file.";
				r.Close();
				fs.Close();
				return null;
			}
			// 004 1-byte flags:
			byte flags = r.ReadByte();
			/*
			 * bit 7: 0=reset-based, 1=savestate-based
			 * other bits: unknown, set to 0
			*/
			if ((int)(flags & 0x80) != 0)
			{
				errorMsg = "Movies that begin with a savestate are not supported.";
				r.Close();
				fs.Close();
				return null;
			}
			// 005 1-byte flags:
			flags = r.ReadByte();
			/*
			 * bit 5: is a FDS recording
			 * bit 6: uses controller 2
			 * bit 7: uses controller 1
			 * other bits: unknown, set to 0
			*/
			bool FDS;
			if ((int)(flags & 0x20) != 0)
			{
				FDS = true;
				m.Header.SetHeaderLine(MovieHeader.PLATFORM, "FDS");
			}
			else
			{
				FDS = false;
				m.Header.SetHeaderLine(MovieHeader.PLATFORM, "NES");
			}
			bool controller2 = false;
			if ((int)(flags & 0x40) != 0)
			{
				controller2 = true;
			}
			bool controller1 = false;
			if ((int)(flags & 0x80) != 0)
			{
				controller1 = true;
			}
			// 006 4-byte little-endian unsigned int: unknown, set to 00000000
			r.ReadInt32();
			// 00A 4-byte little-endian unsigned int: rerecord count minus 1
			uint rerecordCount = r.ReadUInt32();
			/*
			The rerecord count stored in the file is the number of times a savestate was loaded. If a savestate was never loaded,
			the number is 0. Famtasia however displays "1" in such case. It always adds 1 to the number found in the file.
			*/
			m.SetRerecords(((int)rerecordCount) + 1);
			// 00E 2-byte little-endian unsigned int: unknown, set to 0000
			r.ReadInt16();
			// 010 64-byte zero-terminated emulator identifier string
			string emuVersion = RemoveNull(BytesToString(r, 64));
			m.Header.Comments.Add("emuOrigin Famtasia version " + emuVersion);
			// 050 64-byte zero-terminated movie title string
			string gameName = RemoveNull(BytesToString(r, 64));
			m.Header.SetHeaderLine(MovieHeader.GAMENAME, gameName);
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
			/*
			090 frame data begins here
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
			for (long frame = 1; frame <= frames; frame++)
			{
				SimpleController controllers = new SimpleController();
				controllers.Type = new ControllerDefinition();
				controllers.Type.Name = "NES Controller";
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
						byte controllerstate = r.ReadByte();
						byte and = 0x1;
						for (int button = 0; button < buttons.Length; button++)
						{
							controllers["P" + player + " " + buttons[button]] = ((int)(controllerstate & and) != 0);
							and <<= 1;
						}
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
			string signature = BytesToString(r, 4);
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

		// MCM file format: http://code.google.com/p/mednafen-rr/wiki/MCM
		private static Movie ImportMCM(string path, out string errorMsg, out string warningMsg)
		{
			errorMsg = "";
			warningMsg = "";
			Movie m = new Movie(Path.ChangeExtension(path, ".tas"), MOVIEMODE.PLAY);
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
			string signature = BytesToString(r, 4);
			if (signature != "MMV\0")
			{
				errorMsg = "This is not a valid .MMV file.";
				r.Close();
				fs.Close();
				return null;
			}
			// 0004: 4-byte little endian unsigned int: dega version
			uint emuVersion = r.ReadUInt32();
			m.Header.Comments.Add("emuOrigin Dega version " + emuVersion.ToString());
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
			string author = RemoveNull(BytesToString(r, 64));
			m.Header.SetHeaderLine(MovieHeader.AUTHOR, author);
			// 0060: 4-byte little endian flags
			byte flags = r.ReadByte();
			/*
			 * bit 0: unused
			 * bit 1: PAL
			 * bit 2: Japan
			 * bit 3: Game Gear (version 1.16+)
			*/
			bool pal = false;
			if ((int)(flags & 2) != 0)
				pal = true;
			m.Header.SetHeaderLine("PAL", pal.ToString());
			bool japan = false;
			if ((int)(flags & 4) != 0)
				japan = true;
			m.Header.SetHeaderLine("Japan", japan.ToString());
			bool gamegear;
			if ((int)(flags & 8) != 0)
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
			string gameName = RemoveNull(BytesToString(r, 128));
			m.Header.SetHeaderLine(MovieHeader.GAMENAME, gameName);
			// 00e4-00f3: binary: rom MD5 digest
			string MD5 = BytesToString(r, 16, true);
			m.Header.SetHeaderLine("MD5", MD5);
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
				SimpleController controllers = new SimpleController();
				controllers.Type = new ControllerDefinition();
				controllers.Type.Name = "SMS Controller";
				/*
				Controller data is made up of one input packet per frame. Each packet currently consists of 2 bytes. The
				first byte is for controller 1 and the second controller 2. The Game Gear only uses the controller 1 input
				however both bytes are still present.
				*/
				for (int player = 1; player <= 2; player++)
				{
					byte controllerstate = r.ReadByte();
					byte and = 1;
					for (int button = 0; button < buttons.Length; button++)
					{
						controllers["P" + player + " " + buttons[button]] = ((int)(controllerstate & and) != 0);
						and <<= 1;
					}
					if (player == 1)
						controllers["Pause"] = (
							((int)(controllerstate & 0x40) != 0 && (!gamegear)) ||
							((int)(controllerstate & 0x80) != 0 && gamegear)
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
			if ((int)(ControllerFlags & 16) != 0)
				numControllers = 5;
			else if ((int)(ControllerFlags & 8) != 0)
				numControllers = 4;
			else if ((int)(ControllerFlags & 4) != 0)
				numControllers = 3;
			else if ((int)(ControllerFlags & 2) != 0)
				numControllers = 2;
			else
				numControllers = 1;

			byte MovieFlags = r.ReadByte();

			if ((int)(MovieFlags & 1) == 0)
				return null; //TODO: Savestate movies not supported error

			if ((int)(MovieFlags & 2) != 0)
			{
				m.Header.SetHeaderLine("PAL", "True");
			}

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
			//Converts vbm to native text based format.
			Movie m = new Movie(Path.ChangeExtension(path, ".tas"), MOVIEMODE.PLAY);

			FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
			BinaryReader r = new BinaryReader(fs);

			//0xoffset
			//0x00
			uint signature = r.ReadUInt32();  //always 56 42 4D 1A  (VBM\x1A)
			if (signature != 0x56424D1A)
			{
				errorMsg = "This is not a valid .VBM file.";
				r.Close();
				fs.Close();
				return null;
			}

			uint versionno = r.ReadUInt32();  //always 1
			uint uid = r.ReadUInt32();		//time of recording
			m.Header.SetHeaderLine(MovieHeader.GUID, uid.ToString());
			uint frameCount = r.ReadUInt32();

			//0x10
			uint rerecordCount = r.ReadUInt32();
			m.SetRerecords((int)rerecordCount);
			m.Header.SetHeaderLine(MovieHeader.RERECORDS, m.Rerecords.ToString());
			Byte moviestartflags = r.ReadByte();

			bool startfromquicksave = false;
			bool startfromsram = false;

			if ((moviestartflags & 0x01) != 0) startfromquicksave = true;
			if ((moviestartflags & 0x02) != 0) startfromsram = true;

			if (startfromquicksave & startfromsram)
			{
				errorMsg = "Movies that begin with a savestate are not supported.";
				r.Close();
				fs.Close();
				return null;
			}

			//0x15
			Byte controllerflags = r.ReadByte();

			int numControllers;                   //number of controllers changes for SGB

			if ((controllerflags & 0x08) != 0) numControllers = 4;
			else if ((controllerflags & 0x04) != 0) numControllers = 3;
			else if ((controllerflags & 0x02) != 0) numControllers = 2;
			else numControllers = 1;

			//0x16
			Byte systemflags = r.ReadByte();    //what system is it?

			bool is_gba = false;
			bool is_gbc = false;
			bool is_sgb = false;
			bool is_gb = false;

			if ((systemflags & 0x04) != 0) is_sgb = true;
			if ((systemflags & 0x02) != 0) is_gbc = true;
			if ((systemflags & 0x01) != 0) is_gba = true;
			else is_gb = true;

			if (is_gb & is_gbc & is_gba & is_sgb)
			{
				errorMsg = "Not a valid .VBM platform type.";
				r.Close();
				fs.Close();
				return null;
			}
			//TODO: set platform in header

			//0x17
			Byte flags = r.ReadByte();  //emulation flags

			//placeholder for reserved bit (set to 0)
			bool echoramfix = false;
			bool gbchdma5fix = false;
			bool lagreduction = false;
			//placeholder for unsupported bit
			bool rtcenable = false;
			bool skipbiosfile = false;
			bool usebiosfile = false;

			if ((flags & 0x40) != 0) echoramfix = true;
			if ((flags & 0x20) != 0) gbchdma5fix = true;
			if ((flags & 0x10) != 0) lagreduction = true;
			if ((flags & 0x08) != 0)
			{
				errorMsg = "Invalid .VBM file.";
				r.Close();
				fs.Close();
				return null;
			}
			if ((flags & 0x04) != 0) rtcenable = true;
			if ((flags & 0x02) != 0) skipbiosfile = true;
			if ((flags & 0x01) != 0) usebiosfile = true;

			//0x18
			uint winsavetype = r.ReadUInt32();
			uint winflashsize = r.ReadUInt32();

			//0x20
			uint gbemulatortype = r.ReadUInt32();

			char[] internalgamename = r.ReadChars(0x0C);
			string gamename = new String(internalgamename);
			m.Header.SetHeaderLine(MovieHeader.GAMENAME, gamename);

			//0x30
			Byte minorversion = r.ReadByte();
			Byte internalcrc = r.ReadByte();
			ushort internalchacksum = r.ReadUInt16();
			uint unitcode = r.ReadUInt32();
			uint saveoffset = r.ReadUInt32();		//set to 0 if unused
			uint controllerdataoffset = r.ReadUInt32();

			//0x40  start info.
			char[] authorsname = r.ReadChars(0x40);		//vbm specification states these strings
			string author = new String(authorsname);	//are locale dependant.
			m.Header.SetHeaderLine(MovieHeader.AUTHOR, author);

			//0x80
			char[] moviedescription = r.ReadChars(0x80);

			//0x0100
			//End of VBM header

			//if there is no SRAM or savestate, the controller data should start at 0x0100 by default,
			//but this is not buaranteed

			//TODO: implement start data. There are no specifics on the googlecode page as to how long
			//the SRAM or savestate should be.

			uint framesleft = frameCount;

			r.BaseStream.Position = controllerdataoffset;    //advances to controller data.

			int currentoffset = (int)controllerdataoffset;

			SimpleController controllers = new SimpleController();
			controllers.Type = new ControllerDefinition();
			controllers.Type.Name = "Gameboy Controller";
			string[] buttons = new string[8] {"A", "B", "Select", "Start", "Right", "Left", "Up", "Down"};

			for (int frame = 1; frame <= frameCount; frame++)
			{
				ushort controllerstate = r.ReadUInt16();
				// TODO: reset, GBA buttons go here
				byte and = 0x1;
				for (int button = 0; button < buttons.Length; button++)
				{
					controllers["P1 " + buttons[button]] = ((int)(controllerstate & and) != 0);
					and <<= 1;
				}
			}
			MnemonicsGenerator mg = new MnemonicsGenerator();
			mg.SetSource(controllers);
			m.AppendFrame(mg.GetControllersAsMnemonic());
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
			string signature = BytesToString(r, 12);
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