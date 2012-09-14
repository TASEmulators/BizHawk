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
		public const string COREORIGIN = "CoreOrigin";
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
					case ".LSMV":
						m = ImportLSMV(path, out errorMsg, out warningMsg);
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
					case ".NMV":
						m = ImportNMV(path, out errorMsg, out warningMsg);
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
					case ".ZMV":
						m = ImportZMV(path, out errorMsg, out warningMsg);
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
			string[] extensions = new string[13] {
				"FCM", "FM2", "FMV", "GMV", "MCM", "MC2", "MMV", "NMV", "LSMV", "SMV", "VBM", "VMV", "ZMV"
			};
			foreach (string ext in extensions)
				if (extension.ToUpper() == "." + ext)
					return true;
			return false;
		}

		// Reduce all whitespace to single spaces.
		private static string SingleSpaces(string line)
		{
			line = line.Replace("\t", " ");
			line = line.Replace("\n", " ");
			line = line.Replace("\r", " ");
			line = line.Replace("\r\n", " ");
			string prev;
			do
			{
				prev = line;
				line = line.Replace("  ", " ");
			}
			while (prev != line);
			return line;
		}

		// Import a frame from a text-based format.
		private static Movie ImportTextFrame(string line, int lineNum, Movie m, string path, ref string warningMsg,
			ref string errorMsg)
		{
			string[] buttons = new string[] { };
			string controller = "";
			switch (Path.GetExtension(path).ToUpper())
			{
				case ".FM2":
					buttons = new string[8] { "Right", "Left", "Down", "Up", "Start", "Select", "B", "A" };
					controller = "NES Controller";
					break;
				case ".MC2":
					buttons = new string[8] { "Up", "Down", "Left", "Right", "B1", "B2", "Run", "Select" };
					controller = "PC Engine Controller";
					break;
				case ".LSMV":
					buttons = new string[12] {
						"B", "Y", "Select", "Start", "Up", "Down", "Left", "Right", "A", "X", "L", "R"
					};
					controller = "SNES Controller";
					break;
			}
			SimpleController controllers = new SimpleController();
			controllers.Type = new ControllerDefinition();
			controllers.Type.Name = controller;
			MnemonicsGenerator mg = new MnemonicsGenerator();
			// Split up the sections of the frame.
			string[] sections = line.Split('|');
			if (Path.GetExtension(path).ToUpper() == ".FM2" && sections.Length >= 2 && sections[1].Length != 0)
			{
				controllers["Reset"] = (sections[1][0] == '1');
				// Get the first invalid command warning message that arises.
				if (warningMsg == "")
				{
					switch (sections[1][0])
					{
						case '0':
							break;
						case '1':
							break;
						case '2':
							if (m.Frames != 0)
								warningMsg = "hard reset";
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
			}
			if (Path.GetExtension(path).ToUpper() == ".LSMV" && sections.Length != 0)
			{
				string flags = sections[0];
				char[] off = { '.', ' ', '\t', '\n', '\r' };
				if (flags.Length == 0 || off.Contains(flags[0]))
				{
					errorMsg = "Subframes are not supported.";
					return null;
				}
				bool reset = (flags.Length >= 2 && !off.Contains(flags[1]));
				flags = SingleSpaces(flags.Substring(2));
				if (reset && ((flags.Length >= 2 && flags[1] != '0') || (flags.Length >= 4 && flags[3] != '0')))
				{
					errorMsg = "Delayed resets are not supported.";
					return null;
				}
				controllers["Reset"] = reset;
			}
			/*
			 Skip the first two sections of the split, which consist of everything before the starting | and the command.
			 Do not use the section after the last |. In other words, get the sections for the players.
			*/
			int start = 2;
			int end = sections.Length - 1;
			int player_offset = -1;
			if (Path.GetExtension(path).ToUpper() == ".LSMV")
			{
				// LSNES frames don't start or end with a |.
				start--;
				end++;
				player_offset++;
			}
			for (int section = start; section < end; section++)
			{
				// The player number is one less than the section number for the reasons explained above.
				int player = section + player_offset;
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
			mg.SetSource(controllers);
			m.AppendFrame(mg.GetControllersAsMnemonic());
			return m;
		}

		// Import a subtitle from a text-based format.
		private static Movie ImportTextSubtitle(string line, Movie m, string path)
		{
			line = SingleSpaces(line);
			// The header name, frame, and message are separated by whitespace.
			int first = line.IndexOf(' ');
			int second = line.IndexOf(' ', first + 1);
			if (first != -1 && second != -1)
			{
				// Concatenate the frame and message with default values for the additional fields.
				string frame;
				string message;
				string length;
				if (Path.GetExtension(path).ToUpper() != ".LSMV")
				{
					frame = line.Substring(first + 1, second - first - 1);
					length = "200";
				}
				else
				{
					frame = line.Substring(0, first);
					length = line.Substring(first + 1, second - first - 1);
				}
				message = line.Substring(second + 1).Trim();
				m.Subtitles.AddSubtitle("subtitle " + frame + " 0 0 " + length + " FFFFFFFF " + message);
			}
			return m;
		}

		// Import a text-based movie format. This works for .FM2 and .MC2.
		private static Movie ImportText(string path, out string errorMsg, out string warningMsg)
		{
			errorMsg = "";
			warningMsg = "";
			Movie m = new Movie(path + "." + Global.Config.MovieExtension);
			FileInfo file = new FileInfo(path);
			StreamReader sr = file.OpenText();
			string emulator = "";
			string platform = "";
			switch (Path.GetExtension(path).ToUpper())
			{
				case ".FM2":
					emulator = "FCEUX";
					platform = "NES";
					break;
				case ".MC2":
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
				else if (line[0] == '|')
				{
					m = ImportTextFrame(line, lineNum, m, path, ref warningMsg, ref errorMsg);
					if (errorMsg != "")
					{
						sr.Close();
						return null;
					}
				}
				else if (line.ToLower().StartsWith("sub"))
					m = ImportTextSubtitle(line, m, path);
				else if (line.ToLower().StartsWith("emuversion"))
					m.Header.Comments.Add(EMULATIONORIGIN + " " + emulator + " version " + ParseHeader(line, "emuVersion"));
				else if (line.ToLower().StartsWith("version"))
				{
					string version = ParseHeader(line, "version");
					m.Header.Comments.Add(
						MOVIEORIGIN + " " + Path.GetExtension(path) + " version " + version
					);
					if (Path.GetExtension(path).ToUpper() == ".FM2" && version != "3")
					{
						errorMsg = ".FM2 movie version must always be 3.";
						sr.Close();
						return null;
					}
				}
				else if (line.ToLower().StartsWith("romfilename"))
					m.Header.SetHeaderLine(MovieHeader.GAMENAME, ParseHeader(line, "romFilename"));
				else if (line.ToLower().StartsWith("romchecksum"))
				{
					string blob = ParseHeader(line, "romChecksum");
					byte[] MD5 = DecodeBlob(blob);
					if (MD5 != null && MD5.Length == 16)
						m.Header.SetHeaderLine("MD5", BizHawk.Util.BytesToHexString(MD5).ToLower());
					else
						warningMsg = "Bad ROM checksum.";
				}
				else if (line.ToLower().StartsWith("comment author"))
					m.Header.SetHeaderLine(MovieHeader.AUTHOR, ParseHeader(line, "comment author"));
				else if (line.ToLower().StartsWith("rerecordcount"))
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
					m.Rerecords = rerecordCount;
				}
				else if (line.ToLower().StartsWith("guid"))
					m.Header.SetHeaderLine(MovieHeader.GUID, ParseHeader(line, "guid"));
				else if (line.ToLower().StartsWith("startsfromsavestate"))
				{
					// If this movie starts from a savestate, we can't support it.
					if (ParseHeader(line, "StartsFromSavestate") == "1")
					{
						errorMsg = "Movies that begin with a savestate are not supported.";
						sr.Close();
						return null;
					}
				}
				else if (line.ToLower().StartsWith("palflag"))
				{
					bool pal = (ParseHeader(line, "palFlag") == "1");
					m.Header.SetHeaderLine("PAL", pal.ToString());
				}
				else if (line.ToLower().StartsWith("fourscore"))
				{
					bool fourscore = (ParseHeader(line, "fourscore") == "1");
					m.Header.SetHeaderLine(MovieHeader.FOURSCORE, fourscore.ToString());
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
			// Case-insensitive search.
			int x = line.ToLower().LastIndexOf(
				headerName.ToLower()
			) + headerName.Length;
			str = line.Substring(x + 1, line.Length - x - 1);
			return str.Trim();
		}

		// Decode a blob used in FM2 (base64:..., 0x123456...)
		private static byte[] DecodeBlob(string blob)
		{
			if (blob.Length < 2)
				return null;
			if (blob[0] == '0' && (blob[1] == 'x' || blob[1] == 'X'))
				// hex
				return BizHawk.Util.HexStringToBytes(blob.Substring(2));
			else {
				// base64
				if(!blob.ToLower().StartsWith("base64:"))
					return null;
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

		// Ends the string where a NULL character is found.
		private static string NullTerminated(string str)
		{
			int pos = str.IndexOf('\0');
			if (pos != -1)
				str = str.Substring(0, pos);
			return str;
		}

		// FCM file format: http://code.google.com/p/fceu/wiki/FCM
		private static Movie ImportFCM(string path, out string errorMsg, out string warningMsg)
		{
			errorMsg = "";
			warningMsg = "";
			Movie m = new Movie(path + "." + Global.Config.MovieExtension);
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
			 * if "1", "PAL" timing
			 Starting with version 0.98.12 released on September 19, 2004, a "PAL" flag was added to the header but
			 unfortunately it is not reliable - the emulator does not take the "PAL" setting from the ROM, but from a user
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
			m.Rerecords = (int)rerecordCount;
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
			string gameName = Encoding.UTF8.GetString(gameBytes.ToArray());
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
			string author = Encoding.UTF8.GetString(authorBytes.ToArray());
			m.Header.SetHeaderLine(MovieHeader.AUTHOR, author);
			// Advance to first byte of input data.
			r.BaseStream.Position = firstFrameOffset;
			SimpleController controllers = new SimpleController();
			controllers.Type = new ControllerDefinition();
			controllers.Type.Name = "NES Controller";
			MnemonicsGenerator mg = new MnemonicsGenerator();
			string[] buttons = new string[8] { "A", "B", "Select", "Start", "Up", "Down", "Left", "Right" };
			bool fds = false;
			bool fourscore = false;
			int frame = 1;
			while (frame <= frameCount)
			{
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
				frame += frames;
				while (frames > 0)
				{
					m.AppendFrame(mnemonic);
					if (controllers["Reset"])
					{
						controllers["Reset"] = false;
						mnemonic = mg.GetControllersAsMnemonic();
					}
					frames--;
				}
				if (((update >> 7) & 1) == 1)
				{
					// Control update: 1aabbbbb
					bool reset = false;
					int command = update & 0x1F;
					// bbbbb:
					controllers["Reset"] = (command == 1);
					if (warningMsg == "")
					{
						switch (command)
						{
							// Do nothing
							case 0:
								break;
							// Reset
							case 1:
								reset = true;
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
					if (player > 2)
						fourscore = true;
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
			Movie m = new Movie(path + "." + Global.Config.MovieExtension);
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
			m.Rerecords = ((int)rerecordCount) + 1;
			// 00E 2-byte little-endian unsigned int: unknown, set to 0000
			r.ReadInt16();
			// 010 64-byte zero-terminated emulator identifier string
			string emuVersion = NullTerminated(r.ReadStringFixedAscii(64));
			m.Header.Comments.Add(EMULATIONORIGIN + " Famtasia version " + emuVersion);
			m.Header.Comments.Add(MOVIEORIGIN + " .FMV");
			// 050 64-byte zero-terminated movie title string
			string description = NullTerminated(r.ReadStringFixedAscii(64));
			m.Header.Comments.Add(COMMENT + " " + description);
			if (!controller1 && !controller2 && !FDS)
			{
				warningMsg = "No input recorded.";
				r.Close();
				fs.Close();
				return m;
			}
			/*
			 The file format has no means of identifying NTSC/"PAL". It is always assumed that the game is NTSC - that is, 60
			 fps.
			*/
			m.Header.SetHeaderLine("PAL", "False");
			// 090 frame data begins here
			SimpleController controllers = new SimpleController();
			controllers.Type = new ControllerDefinition();
			controllers.Type.Name = "NES Controller";
			MnemonicsGenerator mg = new MnemonicsGenerator();
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
			bool[] masks = new bool[3] { controller1, controller2, FDS };
			/*
			 The file has no terminator byte or frame count. The number of frames is the <filesize minus 144> divided by
			 <number of bytes per frame>.
			*/
			int bytesPerFrame = 0;
			for (int player = 1; player <= masks.Length; player++)
				if (masks[player - 1])
					bytesPerFrame++;
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
						continue;
					byte controllerState = r.ReadByte();
					if (player != 3)
						for (int button = 0; button < buttons.Length; button++)
							controllers["P" + player + " " + buttons[button]] = (((controllerState >> button) & 1) == 1);
					else
						warningMsg = "FDS commands are not properly supported.";
				}
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
			Movie m = new Movie(path + "." + Global.Config.MovieExtension);
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
			m.Header.Comments.Add(EMULATIONORIGIN + " Gens");
			// 010 4-byte little-endian unsigned int: rerecord count
			uint rerecordCount = r.ReadUInt32();
			m.Rerecords = (int)rerecordCount;
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
			 The file format has no means of identifying NTSC/"PAL", but the FPS can still be derived from the header.
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
			string description = NullTerminated(r.ReadStringFixedAscii(40));
			m.Header.Comments.Add(COMMENT + " " + description);
			SimpleController controllers = new SimpleController();
			controllers.Type = new ControllerDefinition();
			controllers.Type.Name = "Genesis 3-Button Controller";
			MnemonicsGenerator mg = new MnemonicsGenerator();
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
			long frameCount = (fs.Length - 64) / 3;
			for (long frame = 1; frame <= frameCount; frame++)
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
				mg.SetSource(controllers);
				m.AppendFrame(mg.GetControllersAsMnemonic());
			}
			return m;
		}

		// LSMV file format: http://tasvideos.org/Lsnes/Movieformat.html
		private static Movie ImportLSMV(string path, out string errorMsg, out string warningMsg)
		{
			errorMsg = "";
			warningMsg = "";
			Movie m = new Movie(path + "." + Global.Config.MovieExtension);
			HawkFile hf = new HawkFile(path);
			// .LSMV movies are .zip files containing data files.
			if (!hf.IsArchive)
			{
				errorMsg = "This is not an archive.";
				return null;
			}
			m.Header.SetHeaderLine(MovieHeader.PLATFORM, "SNES");
			foreach (var item in hf.ArchiveItems)
			{
				if (item.name == "authors")
				{
					hf.BindArchiveMember(item.index);
					var stream = hf.GetStream();
					string authors = Encoding.UTF8.GetString(Util.ReadAllBytes(stream));
					string author_list = "";
					string author_last = "";
					using (StringReader reader = new StringReader(authors))
					{
						string line;
						// Each author is on a different line.
						while ((line = reader.ReadLine()) != null)
						{
							string author = line.Trim();
							if (author != "")
							{
								if (author_last != "")
									author_list += author_last + ", ";
								author_last = author;
							}
						}
					}
					if (author_list != "")
						author_list += "and ";
					if (author_last != "")
						author_list += author_last;
					m.Header.SetHeaderLine(MovieHeader.AUTHOR, author_list);
					hf.Unbind();
				}
				else if (item.name == "coreversion")
				{
					hf.BindArchiveMember(item.index);
					var stream = hf.GetStream();
					string coreversion = Encoding.UTF8.GetString(Util.ReadAllBytes(stream)).Trim();
					m.Header.Comments.Add(COREORIGIN + " " + coreversion);
					hf.Unbind();
				}
				else if (item.name == "gamename")
				{
					hf.BindArchiveMember(item.index);
					var stream = hf.GetStream();
					string gamename = Encoding.UTF8.GetString(Util.ReadAllBytes(stream)).Trim();
					m.Header.SetHeaderLine(MovieHeader.GAMENAME, gamename);
					hf.Unbind();
				}
				else if (item.name == "gametype")
				{
					hf.BindArchiveMember(item.index);
					var stream = hf.GetStream();
					string gametype = Encoding.UTF8.GetString(Util.ReadAllBytes(stream)).Trim();
					// TODO: Handle the other types.
					bool pal = (gametype == "snes_ntsc");
					m.Header.SetHeaderLine("PAL", pal.ToString());
					hf.Unbind();
				}
				else if (item.name == "input")
				{
					hf.BindArchiveMember(item.index);
					var stream = hf.GetStream();
					string input = Encoding.UTF8.GetString(Util.ReadAllBytes(stream));
					int lineNum = 0;
					using (StringReader reader = new StringReader(input))
					{
						lineNum++;
						string line;
						while ((line = reader.ReadLine()) != null)
						{
							if (line == "")
								continue;
							m = ImportTextFrame(line, lineNum, m, path, ref warningMsg, ref errorMsg);
							if (errorMsg != "")
							{
								hf.Unbind();
								return null;
							}
						}
					}
					hf.Unbind();
				}
				else if (item.name.StartsWith("moviesram."))
				{
					errorMsg = "Movies that begin with SRAM are not supported.";
					return null;
				}
				else if (item.name == "rerecords")
				{
					hf.BindArchiveMember(item.index);
					var stream = hf.GetStream();
					string rerecords = Encoding.UTF8.GetString(Util.ReadAllBytes(stream));
					int rerecordCount;
					// Try to parse the re-record count as an integer, defaulting to 0 if it fails.
					try
					{
						rerecordCount = int.Parse(rerecords);
					}
					catch
					{
						rerecordCount = 0;
					}
					m.Rerecords = rerecordCount;
					hf.Unbind();
				}
				else if (item.name == "rom.sha256")
				{
					hf.BindArchiveMember(item.index);
					var stream = hf.GetStream();
					string rom = Encoding.UTF8.GetString(Util.ReadAllBytes(stream)).Trim();
					m.Header.SetHeaderLine("SHA256", rom);
					hf.Unbind();
				}
				else if (item.name == "savestate")
				{
					errorMsg = "Movies that begin with a savestate are not supported.";
					return null;
				}
				else if (item.name == "subtitles")
				{
					hf.BindArchiveMember(item.index);
					var stream = hf.GetStream();
					string subtitles = Encoding.UTF8.GetString(Util.ReadAllBytes(stream));
					using (StringReader reader = new StringReader(subtitles))
					{
						string line;
						while ((line = reader.ReadLine()) != null)
							m = ImportTextSubtitle(line, m, path);
					}
					hf.Unbind();
				}
				else if (item.name == "systemid")
				{
					hf.BindArchiveMember(item.index);
					var stream = hf.GetStream();
					string systemid = Encoding.UTF8.GetString(Util.ReadAllBytes(stream)).Trim();
					m.Header.Comments.Add(EMULATIONORIGIN + " " + systemid);
					hf.Unbind();
				}
			}
			return m;
		}

		/*
		 MCM file format: http://code.google.com/p/mednafen-rr/wiki/MCM
		 Mednafen-rr switched to MC2 from r261, so see r260 for details.
		*/
		private static Movie ImportMCM(string path, out string errorMsg, out string warningMsg)
		{
			errorMsg = "";
			warningMsg = "";
			Movie m = new Movie(path + "." + Global.Config.MovieExtension);
			FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
			BinaryReader r = new BinaryReader(fs);
			// 000 8-byte    "MDFNMOVI" signature
			string signature = r.ReadStringFixedAscii(8);
			if (signature != "MDFNMOVI")
			{
				errorMsg = "This is not a valid .MCM file.";
				r.Close();
				fs.Close();
				return null;
			}
			// 008 uint32     Mednafen Version (Current is 0A 08)
			uint emuVersion = r.ReadUInt32();
			m.Header.Comments.Add(EMULATIONORIGIN + " Mednafen " + emuVersion.ToString());
			// 00C uint32     Movie Format Version (Current is 01)
			uint version = r.ReadUInt32();
			m.Header.Comments.Add(MOVIEORIGIN + " .MCM version " + version);
			// 010 32-byte    MD5 of the ROM used
			byte[] MD5 = r.ReadBytes(16);
			// Discard the second 16 bytes.
			r.ReadBytes(16);
			m.Header.SetHeaderLine("MD5", BizHawk.Util.BytesToHexString(MD5).ToLower());
			// 030 64-byte    Filename of the ROM used (with extension)
			string gameName = NullTerminated(r.ReadStringFixedAscii(64));
			m.Header.SetHeaderLine(MovieHeader.GAMENAME, gameName);
			// 070 uint32     Re-record Count
			uint rerecordCount = r.ReadUInt32();
			m.Rerecords = (int)rerecordCount;
			// 074 5-byte     Console indicator (pce, ngp, pcfx, wswan)
			string platform = NullTerminated(r.ReadStringFixedAscii(5));
			Dictionary<string, Dictionary<string, object>> platforms = new Dictionary<string, Dictionary<string, object>>()
			{
				{
					/*
					 Normally, NES receives from 5 input ports, where the first 4 have a length of 1 byte, and the last has a
					 length of 0. For the sake of simplicity, it is interpreted as 4 ports of 1 byte length for re-recording.
					*/
					"nes", new Dictionary<string, object>
					{
						{"name", "NES"}, {"ports", 4}, {"bytesPerPort", 1},
						{"buttons", new string[8] { "A", "B", "Select", "Start", "Up", "Down", "Left", "Right" }}
					}
				},
				{
					"pce", new Dictionary<string, object>
					{
						{"name", "PC Engine"}, {"ports", 5}, {"bytesPerPort", 2},
						{"buttons", new string[8] { "B1", "B2", "Select", "Run", "Up", "Right", "Down", "Left" }}
					}
				}
			};
			if (!platforms.ContainsKey(platform))
			{
				errorMsg = "Platform " + platform + " not supported.";
				r.Close();
				fs.Close();
				return null;
			}
			string name = (string)platforms[platform]["name"];
			m.Header.SetHeaderLine(MovieHeader.PLATFORM, name);
			// 079 32-byte    Author name
			string author = NullTerminated(r.ReadStringFixedAscii(32));
			m.Header.SetHeaderLine(MovieHeader.AUTHOR, author);
			// 099 103-byte   Padding 0s
			r.ReadBytes(103);
			// TODO: Verify if NTSC/"PAL" mode used for the movie can be detected or not.
			// 100 variable   Input data
			SimpleController controllers = new SimpleController();
			controllers.Type = new ControllerDefinition();
			controllers.Type.Name = name + " Controller";
			MnemonicsGenerator mg = new MnemonicsGenerator();
			int bytes = 256;
			// The input stream consists of 1 byte for power-on and reset, and then X bytes per each input port per frame.
			if (platform == "nes")
			{
				// Power-on.
				r.ReadByte();
				bytes++;
			}
			string[] buttons = (string[])platforms[platform]["buttons"];
			int ports = (int)platforms[platform]["ports"];
			int bytesPerPort = (int)platforms[platform]["bytesPerPort"];
			// Frame Size (with Control Byte)
			int size = (ports * bytesPerPort) + 1;
			long frameCount = (fs.Length - bytes) / size;
			for (int frame = 1; frame <= frameCount; frame++)
			{
				for (int player = 1; player <= ports; player++)
				{
					if (bytesPerPort == 2)
						// Discard the first byte.
						r.ReadByte();
					ushort controllerState = r.ReadByte();
					for (int button = 0; button < buttons.Length; button++)
						controllers["P" + player + " " + buttons[button]] = (((controllerState >> button) & 1) == 1);
				}
				r.ReadByte();
				if (platform == "nes" && warningMsg == "")
					warningMsg = "Control commands are not properly supported.";
				mg.SetSource(controllers);
				m.AppendFrame(mg.GetControllersAsMnemonic());
			}
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
			Movie m = new Movie(path + "." + Global.Config.MovieExtension);
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
			m.Header.Comments.Add(MOVIEORIGIN + " .MMV");
			// 0008: 4-byte little endian unsigned int: frame count
			uint frameCount = r.ReadUInt32();
			// 000c: 4-byte little endian unsigned int: rerecord count
			uint rerecordCount = r.ReadUInt32();
			m.Rerecords = (int)rerecordCount;
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
			string author = NullTerminated(r.ReadStringFixedAscii(64));
			m.Header.SetHeaderLine(MovieHeader.AUTHOR, author);
			// 0060: 4-byte little endian flags
			byte flags = r.ReadByte();
			// bit 0: unused
			// bit 1: "PAL"
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
			string gameName = NullTerminated(r.ReadStringFixedAscii(128));
			m.Header.SetHeaderLine(MovieHeader.GAMENAME, gameName);
			// 00e4-00f3: binary: rom MD5 digest
			byte[] MD5 = r.ReadBytes(16);
			m.Header.SetHeaderLine("MD5", String.Format("{0:x8}", BizHawk.Util.BytesToHexString(MD5).ToLower()));
			SimpleController controllers = new SimpleController();
			controllers.Type = new ControllerDefinition();
			controllers.Type.Name = "SMS Controller";
			MnemonicsGenerator mg = new MnemonicsGenerator();
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
			Movie m = new Movie(path + "." + Global.Config.MovieExtension);
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
			// 004 4-byte version string (example "0960")
			string emuVersion = r.ReadStringFixedAscii(4);
			m.Header.Comments.Add(EMULATIONORIGIN + " Nintendulator version " + emuVersion);
			m.Header.Comments.Add(MOVIEORIGIN + " .NMV");
			// 008 4-byte file size, not including the 16-byte header
			r.ReadUInt32();
			/*
			 00C 4-byte file type string
			 * "NSAV" - standard savestate
			 * "NREC" - savestate saved during movie recording
			 * "NMOV" - standalone movie file
			*/
			string type = r.ReadStringFixedAscii(4);
			if (type != "NMOV")
			{
				errorMsg = "Movies that begin with a savestate are not supported.";
				r.Close();
				fs.Close();
				return null;
			}
			/*
			 Individual blocks begin with an 8-byte header, consisting of a 4-byte signature and a 4-byte length (which does
			 not include the length of the block header).
			 The final block in the file is of type "NMOV"
			*/
			string header = r.ReadStringFixedAscii(4);
			if (header != "NMOV")
			{
				errorMsg = "This is not a valid .NMV file.";
				r.Close();
				fs.Close();
				return null;
			}
			r.ReadUInt32();
			// 000 1-byte controller #1 type (see below)
			byte controller1 = r.ReadByte();
			// 001 1-byte controller #2 type (or four-score mask, see below)
			byte controller2 = r.ReadByte();
			/*
			 Controller data is variant, depending on which controllers are attached at the time of recording. The following
			 controllers are implemented:
			 * 0 - Unconnected
			 * 1 - Standard Controller (1 byte)
			 * 2 - Zapper (3 bytes)
			 * 3 - Arkanoid Paddle (2 bytes)
			 * 4 - Power Pad (2 bytes)
			 * 5 - Four-Score (special)
			 * 6 - SNES controller (2 bytes) - A/B become B/Y, adds A/X and L/R shoulder buttons
			 * 7 - Vs Unisystem Zapper (3 bytes)
			*/
			bool fourscore = (controller1 == 5);
			m.Header.SetHeaderLine(MovieHeader.FOURSCORE, fourscore.ToString());
			bool[] masks = new bool[5] { false, false, false, false, false };
			if (fourscore)
			{
				/*
				 When a Four-Score is indicated for Controller #1, the Controller #2 byte becomes a bit mask to indicate
				 which ports on the Four-Score have controllers connected to them. Each connected controller stores 1 byte
				 per frame. Nintendulator's Four-Score recording is seemingly broken.
				*/
				for (int controller = 1; controller < masks.Length; controller++)
					masks[controller - 1] = (((controller2 >> (controller - 1)) & 1) == 1);
				warningMsg = "Nintendulator's Four Score recording is seemingly broken.";
			}
			else
			{
				byte[] types = new byte[2] { controller1, controller2 };
				for (int controller = 1; controller <= types.Length; controller++)
				{
					masks[controller - 1] = (types[controller - 1] == 1);
					// Get the first unsupported controller warning message that arises.
					if (warningMsg == "")
					{
						switch (types[controller - 1])
						{
							case 0:
								break;
							case 2:
								warningMsg = "Zapper";
								break;
							case 3:
								warningMsg = "Arkanoid Paddle";
								break;
							case 4:
								warningMsg = "Power Pad";
								break;
							case 5:
								warningMsg = "A Four Score in the second controller port is invalid.";
								continue;
							case 6:
								warningMsg = "SNES controller";
								break;
							case 7:
								warningMsg = "Vs Unisystem Zapper";
								break;
						}
						if (warningMsg != "")
							warningMsg = warningMsg + " is not properly supported.";
					}
				}
			}
			// 002 1-byte expansion port controller type
			byte expansion = r.ReadByte();
			/*
			 The expansion port can potentially have an additional controller connected. The following expansion controllers
			 are implemented:
			 * 0 - Unconnected
			 * 1 - Famicom 4-player adapter (2 bytes)
			 * 2 - Famicom Arkanoid paddle (2 bytes)
			 * 3 - Family Basic Keyboard (currently does not support demo recording)
			 * 4 - Alternate keyboard layout (currently does not support demo recording)
			 * 5 - Family Trainer (2 bytes)
			 * 6 - Oeka Kids writing tablet (3 bytes)
			*/
			string[] expansions = new string[7] {
				"Unconnected", "Famicom 4-player adapter", "Famicom Arkanoid paddle", "Family Basic Keyboard",
				"Alternate keyboard layout", "Family Trainer", "Oeka Kids writing tablet"
			};
			if (expansion != 0 && warningMsg == "")
				warningMsg = "Expansion port is not properly supported. This movie uses " + expansions[expansion] + ".";
			// 003 1-byte number of bytes per frame, plus flags
			byte data = r.ReadByte();
			int bytesPerFrame = data & 0xF;
			int bytes = 0;
			for (int controller = 1; controller < masks.Length; controller++)
				if (masks[controller - 1])
					bytes++;
			/*
			 Depending on the mapper used by the game in question, an additional byte of data may be stored during each
			 frame. This is most frequently used for FDS games (storing either the disk number or 0xFF to eject) or VS
			 Unisystem coin/DIP switch toggles (limited to 1 action per frame). This byte exists if the bytes per frame do
			 not match up with the amount of bytes the controllers take up.
			*/
			if (bytes != bytesPerFrame)
				masks[4] = true;
			// bit 6: Game Genie active
			/*
			 bit 7: Framerate
			 * if "0", NTSC timing
			 * if "1", "PAL" timing
			*/
			bool pal = (((data >> 7) & 1) == 1);
			m.Header.SetHeaderLine("PAL", pal.ToString());
			// 004 4-byte little-endian unsigned int: rerecord count
			uint rerecordCount = r.ReadUInt32();
			m.Rerecords = (int)rerecordCount;
			/*
			 008 4-byte little-endian unsigned int: length of movie description
			 00C (variable) null-terminated UTF-8 text, movie description (currently not implemented)
			*/
			string movieDescription = NullTerminated(r.ReadStringFixedAscii((int)r.ReadUInt32()));
			m.Header.Comments.Add(COMMENT + " " + movieDescription);
			// ... 4-byte little-endian unsigned int: length of controller data in bytes
			uint length = r.ReadUInt32();
			// ... (variable) controller data
			SimpleController controllers = new SimpleController();
			controllers.Type = new ControllerDefinition();
			controllers.Type.Name = "NES Controller";
			MnemonicsGenerator mg = new MnemonicsGenerator();
			/*
			 Standard controllers store data in the following format:
			 * 01: A
			 * 02: B
			 * 04: Select
			 * 08: Start
			 * 10: Up
			 * 20: Down
			 * 40: Left
			 * 80: Right
			 Other controllers store data in their own formats, and are beyond the scope of this document.
			*/
			string[] buttons = new string[8] { "A", "B", "Select", "Start", "Up", "Down", "Left", "Right" };
			// The controller data contains <number_of_bytes> / <bytes_per_frame> frames.
			long frameCount = length / bytesPerFrame;
			for (int frame = 1; frame <= frameCount; frame++)
			{
				// Controller update data is emitted to the movie file during every frame.
				for (int player = 1; player <= masks.Length; player++)
				{
					if (!masks[player - 1])
						continue;
					byte controllerState = r.ReadByte();
					if (player != 5)
						for (int button = 0; button < buttons.Length; button++)
							controllers["P" + player + " " + buttons[button]] = (((controllerState >> button) & 1) == 1);
					else if (warningMsg == "")
						warningMsg = "Extra input is not properly supported.";
				}
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
			// 000 4-byte signature: 53 4D 56 1A "SMV\x1A"
			string signature = r.ReadStringFixedAscii(4);
			if (signature != "SMV\x1A")
			{
				errorMsg = "This is not a valid .SMV file.";
				r.Close();
				fs.Close();
				return null;
			}
			// 004 4-byte little-endian unsigned int: version number
			uint version = r.ReadUInt32();
			Movie m;
			switch (version)
			{
				case 1:
					m = ImportSMV143(r, path, out errorMsg, out warningMsg);
					break;
				case 4:
					m = ImportSMV151(r, path, out errorMsg, out warningMsg);
					break;
				case 5:
					m = ImportSMV152(r, path, out errorMsg, out warningMsg);
					break;
				default:
					errorMsg = "SMV version not recognized. 1.43, 1.51, and 1.52 are currently supported.";
					r.Close();
					fs.Close();
					return null;
			}
			r.Close();
			fs.Close();
			m.Header.SetHeaderLine(MovieHeader.PLATFORM, "SNES");
			return m;
		}

		// SMV 1.43 file format: http://code.google.com/p/snes9x-rr/wiki/SMV143
		private static Movie ImportSMV143(BinaryReader r, string path, out string errorMsg, out string warningMsg)
		{
			errorMsg = "";
			warningMsg = "";
			Movie m = new Movie(path + "." + Global.Config.MovieExtension);
			m.Header.Comments.Add(EMULATIONORIGIN + " Snes9x version 1.43");
			m.Header.Comments.Add(MOVIEORIGIN + " .SMV");
			/*
			 008 4-byte little-endian integer: movie "uid" - identifies the movie-savestate relationship, also used as the
			 recording time in Unix epoch format
			*/
			uint uid = r.ReadUInt32();
			m.Header.SetHeaderLine(MovieHeader.GUID, String.Format("{0:X8}", uid) + "-0000-0000-0000-000000000000");
			// 00C 4-byte little-endian unsigned int: rerecord count
			m.Rerecords = (int)r.ReadUInt32();
			// 010 4-byte little-endian unsigned int: number of frames
			uint frameCount = r.ReadUInt32();
			// 014 1-byte flags "controller mask"
			byte flags = r.ReadByte();
			int players = 0;
			/*
			 * bit 0: controller 1 in use
			 * bit 1: controller 2 in use
			 * bit 2: controller 3 in use
			 * bit 3: controller 4 in use
			 * bit 4: controller 5 in use
			 * other: reserved, set to 0
			*/
			for (int controller = 1; controller <= 5; controller++)
				if (((flags >> (controller - 1)) & 0x1) != 0)
					players++;
			// 015 1-byte flags "movie options"
			flags = r.ReadByte();
			/*
				 bit 0:
					 if "0", movie begins from an embedded "quicksave" snapshot
					 if "1", a SRAM is included instead of a quicksave; movie begins from reset
			*/
			if ((flags & 0x1) == 0)
			{
				errorMsg = "Movies that begin with a savestate are not supported.";
				r.Close();
				return null;
			}
			// bit 1: if "0", movie is NTSC (60 fps); if "1", movie is PAL (50 fps)
			bool pal = (((flags >> 1) & 0x1) != 0);
			m.Header.SetHeaderLine("PAL", pal.ToString());
			// other: reserved, set to 0
			/*
			 016 1-byte flags "sync options":
				 bit 0: MOVIE_SYNC2_INIT_FASTROM
				 other: reserved, set to 0
			*/
			r.ReadByte();
			/*
			 017 1-byte flags "sync options":
				 bit 0: MOVIE_SYNC_DATA_EXISTS
					 if "1", all sync options flags are defined.
					 if "0", all sync options flags have no meaning.
				 bit 1: MOVIE_SYNC_WIP1TIMING
				 bit 2: MOVIE_SYNC_LEFTRIGHT
				 bit 3: MOVIE_SYNC_VOLUMEENVX
				 bit 4: MOVIE_SYNC_FAKEMUTE
				 bit 5: MOVIE_SYNC_SYNCSOUND
				 bit 6: MOVIE_SYNC_HASROMINFO
					 if "1", there is extra ROM info located right in between of the metadata and the savestate.
				 bit 7: set to 0.
			*/
			flags = r.ReadByte();
			/*
			 Extra ROM info is always positioned right before the savestate. Its size is 30 bytes if MOVIE_SYNC_HASROMINFO
			 is used (and MOVIE_SYNC_DATA_EXISTS is set), 0 bytes otherwise.
			*/
			int extraRomInfo = (((flags >> 6) & 0x1) != 0 && (flags & 0x1) != 0) ? 30 : 0;
			// 018 4-byte little-endian unsigned int: offset to the savestate inside file
			uint savestateOffset = r.ReadUInt32();
			// 01C 4-byte little-endian unsigned int: offset to the controller data inside file
			uint firstFrameOffset = r.ReadUInt32();
			/*
			 After the header comes "metadata", which is UTF16-coded movie title string (author info). The metadata begins
			 from position 32 (0x20) and ends at <savestate_offset - length_of_extra_rom_info_in_bytes>.
			*/
			byte[] metadata = r.ReadBytes((int)(savestateOffset - extraRomInfo - 0x20));
			string author = NullTerminated(Encoding.Unicode.GetString(metadata).Trim());
			if (author != "")
				m.Header.SetHeaderLine(MovieHeader.AUTHOR, author);
			if (extraRomInfo == 30)
			{
				// 000 3 bytes of zero padding: 00 00 00 003 4-byte integer: CRC32 of the ROM 007 23-byte ascii string
				r.ReadBytes(3);
				int crc32 = r.ReadInt32(); // TODO: Validate.
				// the game name copied from the ROM, truncated to 23 bytes (the game name in the ROM is 21 bytes)
				string gameName = NullTerminated(Encoding.UTF8.GetString(r.ReadBytes(23)));
				m.Header.SetHeaderLine(MovieHeader.GAMENAME, gameName);
			}
			r.BaseStream.Position = firstFrameOffset;
			SimpleController controllers = new SimpleController();
			controllers.Type = new ControllerDefinition();
			controllers.Type.Name = "SNES Controller";
			MnemonicsGenerator mg = new MnemonicsGenerator();
			/*
			 01 00 (reserved)
			 02 00 (reserved)
			 04 00 (reserved)
			 08 00 (reserved)
			 10 00 R
			 20 00 L
			 40 00 X
			 80 00 A
			 00 01 Right
			 00 02 Left
			 00 04 Down
			 00 08 Up
			 00 10 Start
			 00 20 Select
			 00 40 Y
			 00 80 B
			*/
			string[] buttons = new string[12] {
				"Right", "Left", "Down", "Up", "Start", "Select", "Y", "B", "R", "L", "X", "A"
			};
			for (int frame = 0; frame <= frameCount; frame++)
			{
				controllers["Reset"] = true;
				for (int player = 1; player <= players; player++)
				{
					/*
					 Each frame consists of 2 bytes per controller. So if there are 3 controllers, a frame is 6 bytes and
					 if there is only 1 controller, a frame is 2 bytes.
					*/
					byte controllerState1 = r.ReadByte();
					byte controllerState2 = r.ReadByte();
					/*
					 In the reset-recording patch, a frame that contains the value FF FF for every controller denotes a
					 reset. The reset is done through the S9xSoftReset routine.
					*/
					if (controllerState1 != 0xFF || controllerState2 != 0xFF)
						controllers["Reset"] = false;
					ushort controllerState = (ushort)(((controllerState1 << 4) & 0x0F00) | controllerState2);
					for (int button = 0; button < buttons.Length; button++)
						controllers["P" + player + " " + buttons[button]] = (((controllerState >> button) & 1) == 1);
				}
				// The controller data contains <number_of_frames + 1> frames.
				if (frame == 0)
					continue;
				mg.SetSource(controllers);
				m.AppendFrame(mg.GetControllersAsMnemonic());
			}
			r.Close();
			return m;
		}

		// SMV 1.51 file format: http://code.google.com/p/snes9x-rr/wiki/SMV151
		private static Movie ImportSMV151(BinaryReader r, string path, out string errorMsg, out string warningMsg)
		{
			errorMsg = "";
			warningMsg = "";
			Movie m = new Movie(path + "." + Global.Config.MovieExtension);
			m.Header.Comments.Add(EMULATIONORIGIN + " Snes9x version 1.51");
			m.Header.Comments.Add(MOVIEORIGIN + " .SMV");
			// TODO: Import.
			return m;
		}

		private static Movie ImportSMV152(BinaryReader r, string path, out string errorMsg, out string warningMsg)
		{
			errorMsg = "";
			warningMsg = "";
			Movie m = new Movie(path + "." + Global.Config.MovieExtension);
			uint GUID = r.ReadUInt32();
			m.Header.Comments.Add(EMULATIONORIGIN + " Snes9x version 1.52");
			m.Header.Comments.Add(MOVIEORIGIN + " .SMV");
			// TODO: Import.
			return m;
		}

		// VBM file format: http://code.google.com/p/vba-rerecording/wiki/VBM
		private static Movie ImportVBM(string path, out string errorMsg, out string warningMsg)
		{
			errorMsg = "";
			warningMsg = "";
			Movie m = new Movie(path + "." + Global.Config.MovieExtension);
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
			m.Header.SetHeaderLine(MovieHeader.GUID, String.Format("{0:X8}", uid) + "-0000-0000-0000-000000000000");
			// 00C 4-byte little-endian unsigned int: number of frames
			uint frameCount = r.ReadUInt32();
			// 010 4-byte little-endian unsigned int: rerecord count
			uint rerecordCount = r.ReadUInt32();
			m.Rerecords = (int)rerecordCount;
			// 014 1-byte flags: (movie start flags)
			byte flags = r.ReadByte();
			// bit 0: if "1", movie starts from an embedded "quicksave" snapshot
			bool startfromquicksave = ((flags & 1) == 1);
			// bit 1: if "1", movie starts from reset with an embedded SRAM
			bool startfromsram = (((flags >> 1) & 1) == 1);
			// other: reserved, set to 0
			// (If both bits 0 and 1 are "1", the movie file is invalid)
			if (startfromquicksave && startfromsram)
			{
				errorMsg = "This is not a valid .VBM file.";
				r.Close();
				fs.Close();
				return null;
			}
			if (startfromquicksave)
			{
				errorMsg = "Movies that begin with a savestate are not supported.";
				r.Close();
				fs.Close();
				return null;
			}
			if (startfromsram)
			{
				errorMsg = "Movies that begin with SRAM are not supported.";
				r.Close();
				fs.Close();
				return null;
			}
			// 015 1-byte flags: controller flags
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
			string gameName = NullTerminated(r.ReadStringFixedAscii(12));
			m.Header.SetHeaderLine(MovieHeader.GAMENAME, gameName);
			// 030 1-byte unsigned char: minor version/revision number of current VBM version, the latest is "1"
			byte minorVersion = r.ReadByte();
			m.Header.Comments.Add(MOVIEORIGIN + " .VBM version " + majorVersion + "." + minorVersion);
			m.Header.Comments.Add(EMULATIONORIGIN + " Visual Boy Advance");
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
			string author = NullTerminated(r.ReadStringFixedAscii(64));
			m.Header.SetHeaderLine(MovieHeader.AUTHOR, author);
			// The following 128 bytes are for a description of the movie. Both parts must be null-terminated.
			string movieDescription = NullTerminated(r.ReadStringFixedAscii(128));
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
			MnemonicsGenerator mg = new MnemonicsGenerator();
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
				mg.SetSource(controllers);
				m.AppendFrame(mg.GetControllersAsMnemonic());
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
			Movie m = new Movie(path + "." + Global.Config.MovieExtension);
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
			// 00C 2-byte little-endian integer: movie version 0x0400
			ushort version = r.ReadUInt16();
			m.Header.Comments.Add(MOVIEORIGIN + " .VMV version " + version);
			m.Header.Comments.Add(EMULATIONORIGIN + " VirtuaNES");
			// 00E 2-byte little-endian integer: record version
			ushort recordVersion = r.ReadUInt16();
			m.Header.Comments.Add(COMMENT + " Record version " + recordVersion);
			// 010 4-byte flags (control byte)
			uint flags = r.ReadUInt32();
			// bit 0: controller 1 in use
			bool controller1 = ((flags & 1) == 1);
			// bit 1: controller 2 in use
			bool controller2 = (((flags >> 1) & 1) == 1);
			// bit 2: controller 3 in use
			bool controller3 = (((flags >> 2) & 1) == 1);
			// bit 3: controller 4 in use
			bool controller4 = (((flags >> 3) & 1) == 1);
			bool fourscore = (controller3 || controller4);
			m.Header.SetHeaderLine(MovieHeader.FOURSCORE, fourscore.ToString());
			/*
			 bit 6: 1=reset-based, 0=savestate-based (movie version <= 0x300 is always savestate-based)
			 If the movie version is < 0x400, or the "from-reset" flag is not set, a savestate is loaded from the movie.
			 Otherwise, the savestate is ignored.
			*/
			if (version < 0x400 || ((flags >> 6) & 1) == 0)
			{
				errorMsg = "Movies that begin with a savestate are not supported.";
				r.Close();
				fs.Close();
				return null;
			}
			/*
			 bit 7: disable rerecording
			 Other bits: reserved, set to 0
			*/
			/*
			 014 DWORD   Ext0;                   // ROM:program CRC  FDS:program ID
			 018 WORD    Ext1;                   // ROM:unused,0     FDS:maker ID
			 01A WORD    Ext2;                   // ROM:unused,0     FDS:disk no.
			*/
			r.ReadUInt64();
			// 01C 4-byte little-endian integer: rerecord count
			uint rerecordCount = r.ReadUInt32();
			m.Rerecords = (int)rerecordCount;
			/*
			020 BYTE    RenderMethod
			0=POST_ALL,1=PRE_ALL
			2=POST_RENDER,3=PRE_RENDER
			4=TILE_RENDER
			021 BYTE    IRQtype                 // IRQ type
			022 BYTE    FrameIRQ                // FrameIRQ not allowed
			*/
			r.ReadBytes(3);
			// 023 1-byte flag: 0=NTSC (60 Hz), 1="PAL" (50 Hz)
			bool pal = (r.ReadByte() == 1);
			m.Header.SetHeaderLine("PAL", pal.ToString());
			/*
			 024 8-bytes: reserved, set to 0
			 02C 4-byte little-endian integer: save state start offset
			 030 4-byte little-endian integer: save state end offset
			*/
			r.ReadBytes(16);
			// 034 4-byte little-endian integer: movie data offset
			uint firstFrameOffset = r.ReadUInt32();
			// 038 4-byte little-endian integer: movie frame count
			uint frameCount = r.ReadUInt32();
			// 03C 4-byte little-endian integer: CRC (CRC excluding this data(to prevent cheating))
			r.ReadUInt32();
			if (!controller1 && !controller2 && !controller3 && !controller4)
			{
				warningMsg = "No input recorded.";
				r.Close();
				fs.Close();
				return m;
			}
			r.BaseStream.Position = firstFrameOffset;
			SimpleController controllers = new SimpleController();
			controllers.Type = new ControllerDefinition();
			controllers.Type.Name = "NES Controller";
			MnemonicsGenerator mg = new MnemonicsGenerator();
			/*
			 * 01 A
			 * 02 B
			 * 04 Select
			 * 08 Start
			 * 10 Up
			 * 20 Down
			 * 40 Left
			 * 80 Right
			*/
			string[] buttons = new string[8] { "A", "B", "Select", "Start", "Up", "Down", "Left", "Right" };
			bool[] masks = new bool[4] {controller1, controller2, controller3, controller4};
			for (int frame = 1; frame <= frameCount; frame++)
			{
				/*
				 For the other control bytes, if a key from 1P to 4P (whichever one) is entirely ON, the following 4 bytes
				 becomes the controller data (TODO: Figure out what this means).
				 Each frame consists of 1 or more bytes. Controller 1 takes 1 byte, controller 2 takes 1 byte, controller 3
				 takes 1 byte, and controller 4 takes 1 byte. If all four exist, the frame is 4 bytes. For example, if the
				 movie only has controller 1 data, a frame is 1 byte.
				*/
				for (int player = 1; player <= masks.Length; player++)
				{
					if (!masks[player - 1])
						continue;
					// TODO: Check for commands: Lines 207-239 of Nesmock.
					byte controllerState = r.ReadByte();
					for (int button = 0; button < buttons.Length; button++)
						controllers["P" + player + " " + buttons[button]] = (((controllerState >> button) & 1) == 1);
				}
				mg.SetSource(controllers);
				m.AppendFrame(mg.GetControllersAsMnemonic());
			}
			r.Close();
			fs.Close();
			return m;
		}

		// ZMV file format: http://tasvideos.org/ZMV.html
		private static Movie ImportZMV(string path, out string errorMsg, out string warningMsg)
		{
			errorMsg = "";
			warningMsg = "";
			Movie m = new Movie(path + "." + Global.Config.MovieExtension);
			// TODO: Import.
			return m;
		}
	}
}
