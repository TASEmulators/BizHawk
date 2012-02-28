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
			string[] extensions = new string[9] { "FCM", "FM2", "FMV", "GMV", "MC2", "MMV", "SMV", "TAS", "VBM" };
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
			var file = new FileInfo(path);
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
			using (StreamReader sr = file.OpenText())
			{
				int lineNum = 0;
				string line = "";
				string rerecordCount = "";
				while ((line = sr.ReadLine()) != null)
				{
					lineNum++;
					if (line == "")
						continue;
					if (line.Contains("rerecordCount"))
					{
						rerecordCount = ParseHeader(line, "rerecordCount");
						// Try to parse the Re-record count as an integer, defaulting to 0 if it fails.
						try
						{
							m.SetRerecords(int.Parse(rerecordCount));
						}
						catch
						{
							m.SetRerecords(0);
						}
					}
					else if (line.Contains("StartsFromSavestate"))
					{
						line = ParseHeader(line, "StartsFromSavestate");
						// If this movie starts from a savestate, we can't support it.
						if (line == "1")
						{
							errorMsg = "Movies that begin with a savestate are not supported.";
							return null;
						}
					}
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
					else if (line.StartsWith("guid"))
						m.Header.SetHeaderLine(MovieHeader.GUID, ParseHeader(line, "GUID"));
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
									warningMsg = "FDS Select";
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
			}
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

			byte[] signatureBytes = new byte[4];
			for (int x = 0; x < 4; x++)
				signatureBytes[x] = r.ReadByte();
			string signature = System.Text.Encoding.UTF8.GetString(signatureBytes);
			if (signature.Substring(0, 3) != "FCM")
			{
				errorMsg = "This is not a valid .FCM file.";
				return null;
			}

			uint version = r.ReadUInt32();
			m.Header.SetHeaderLine(MovieHeader.MovieVersion, "FCEU movie version " + version.ToString() + " (.fcm)");

			byte[] flags = new byte[4];
			for (int x = 0; x < 4; x++)
				flags[x] = r.ReadByte();

			uint frameCount = r.ReadUInt32();

			m.SetRerecords((int)r.ReadUInt32());

			uint movieDataSize = r.ReadUInt32();
			uint savestateOffset = r.ReadUInt32();
			uint firstFrameOffset = r.ReadUInt32();

			byte[] romCheckSum = r.ReadBytes(16);
			//TODO: ROM checksum movie header line (MD5)

			uint emuVersion = r.ReadUInt32();
			m.Header.Comments.Add("emuOrigin FCEU " + emuVersion.ToString());

			List<byte> romBytes = new List<byte>();
			while (true)
			{
				if (r.PeekChar() == 0)
					break;
				else
					romBytes.Add(r.ReadByte());
			}
			string rom = System.Text.Encoding.UTF8.GetString(romBytes.ToArray());
			m.Header.SetHeaderLine(MovieHeader.GAMENAME, rom);

			r.ReadByte(); //Advance past null byte

			List<byte> authorBytes = new List<byte>();
			while (true)
			{
				if (r.PeekChar() == 0)
					break;
				else
					authorBytes.Add(r.ReadByte());
			}
			string author = System.Text.Encoding.UTF8.GetString(authorBytes.ToArray());
			m.Header.SetHeaderLine(MovieHeader.AUTHOR, author);

			r.ReadByte(); //Advance past null byte

			bool movieSyncHackOn = true;
			if ((int)(flags[0] & 16) > 0)
				movieSyncHackOn = false;

			bool pal = false;
			if ((int)(flags[0] & 4) > 0)
				pal = true;

			m.Header.SetHeaderLine("SyncHack", movieSyncHackOn.ToString());
			m.Header.SetHeaderLine("PAL", pal.ToString());

			//Power on vs reset
			if ((int)(flags[0] & 8) > 0)
			{ } //Power-on = default
			else if ((int)(flags[0] & 2) > 0)
			{ } //we don't support start from reset, do some kind of notification here
			else
			{ } //this movie starts from savestate, freak out here

			//Advance to first byte of input data
			//byte[] throwaway = new byte[firstFrameOffset];
			//r.Read(throwaway, 0, (int)firstFrameOffset);
			r.BaseStream.Position = firstFrameOffset;
			//moviedatasize stuff

			//read frame data
			//TODO: special commands like fds disk switch, etc, and power/reset

			//TODO: use stringbuilder class for speed
			//string ButtonLookup = "RLDUSsBARLDUSsBARLDUSsBARLDUSsBA"; //TODO: This assumes input data is the same in fcm as bizhawk, which it isn't
			string frame = "|0|"; //TODO: read reset command rather than hard code it off
			for (int x = 0; x < frameCount; x++)
			{
				byte joy = r.ReadByte();

				//Read each byte of controller one data

				frame += "|";

				r.ReadBytes(3); //Lose remaining controllers for now
				m.AppendFrame(frame);
			}

			//set 4 score flag if necessary
			r.Close();
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
			m.SetRerecords(((int)rerecordCount) - 1);
			// 00E 2-byte little-endian unsigned int: unknown, set to 0000
			r.ReadInt16();
			// 010 64-byte zero-terminated emulator identifier string
			string emuVersion = RemoveNull(BytesToString(r, 64));
			m.Header.Comments.Add("emuOrigin Famtasia version " + emuVersion);
			// 050 64-byte zero-terminated movie title string
			string gameName = RemoveNull(BytesToString(r, 64));
			m.Header.SetHeaderLine(MovieHeader.GAMENAME, gameName);
			// 090 frame data begins here
			if (controller1 || controller2 || FDS)
			{
				// TODO: Frame data handling.
			}
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
				return null;
			}
			// 0004: 4-byte little endian unsigned int: dega version
			uint emuVersion = r.ReadUInt32();
			m.Header.Comments.Add("emuOrigin Dega version " + emuVersion.ToString());
			// 0008: 4-byte little endian unsigned int: frame count
			uint framecount = r.ReadUInt32();
			// 000c: 4-byte little endian unsigned int: rerecord count
			uint rerecordCount = r.ReadUInt32();
			m.SetRerecords((int)rerecordCount);
			// 0010: 4-byte little endian flag: begin from reset?
			uint reset = r.ReadUInt32();
			if (reset == 0)
			{
				errorMsg = "Movies that begin with a savestate are not supported.";
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
			if ((int)(flags & 2) > 0)
				pal = true;
			m.Header.SetHeaderLine("PAL", pal.ToString());
			bool japan = false;
			if ((int)(flags & 4) > 0)
				japan = true;
			m.Header.SetHeaderLine("Japan", japan.ToString());
			bool gamegear;
			if ((int)(flags & 8) > 0)
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
			 * 76543210
			 * bit 0 (0x01): up
			 * bit 1 (0x02): down
			 * bit 2 (0x04): left
			 * bit 3 (0x08): right
			 * bit 4 (0x10): 1
			 * bit 5 (0x20): 2
			 * bit 6 (0x40): start (Master System)
			 * bit 7 (0x80): start (Game Gear)
			*/
			for (int frame = 0; frame < framecount; frame++)
			{
				byte controllerstate;
				SimpleController controllers = new SimpleController();
				controllers.Type = new ControllerDefinition();
				controllers.Type.Name = "SMS Controller";
				string[] buttons = new string[6] { "Up", "Down", "Left", "Right", "B1", "B2" };
				for (int player = 1; player <= 2; player++)
				{
					controllerstate = r.ReadByte();
					byte and = 0x1;
					for (int button = 0; button < buttons.Length; button++)
					{
						controllers["P" + player + " " + buttons[button]] = ((int)(controllerstate & and) > 0);
						and <<= 1;
					}
					if (player == 1)
						controllers["Pause"] = (
							((int)(controllerstate & 0x40) > 0 && (!gamegear)) ||
							((int)(controllerstate & 0x80) > 0 && gamegear)
						);
				}
				MnemonicsGenerator mg = new MnemonicsGenerator();
				mg.SetSource(controllers);
				m.AppendFrame(mg.GetControllersAsMnemonic());
			}
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

			uint framecount = r.ReadUInt32();
			byte ControllerFlags = r.ReadByte();

			int numControllers;
			if ((int)(ControllerFlags & 16) > 0)
				numControllers = 5;
			else if ((int)(ControllerFlags & 8) > 0)
				numControllers = 4;
			else if ((int)(ControllerFlags & 4) > 0)
				numControllers = 3;
			else if ((int)(ControllerFlags & 2) > 0)
				numControllers = 2;
			else
				numControllers = 1;

			byte MovieFlags = r.ReadByte();

			if ((int)(MovieFlags & 1) == 0)
				return null; //TODO: Savestate movies not supported error

			if ((int)(MovieFlags & 2) > 0)
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
			for (int x = 0; x < framecount; x++)
			{
				//TODO: FF FF for all controllers = Reset
				//string frame = "|0|";
				for (int y = 0; y < numControllers; y++)
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
				return null;
			}

			uint versionno = r.ReadUInt32();  //always 1
			uint uid = r.ReadUInt32();		//time of recording
			m.Header.SetHeaderLine(MovieHeader.GUID, uid.ToString());
			uint framecount = r.ReadUInt32();

			//0x10
			uint rerecordCount = r.ReadUInt32();
			m.SetRerecords((int)rerecordCount);
			m.Header.SetHeaderLine(MovieHeader.RERECORDS, m.Rerecords.ToString());
			Byte moviestartflags = r.ReadByte();

			bool startfromquicksave = false;
			bool startfromsram = false;

			if ((moviestartflags & 0x01) > 0) startfromquicksave = true;
			if ((moviestartflags & 0x02) > 0) startfromsram = true;

			if (startfromquicksave & startfromsram)
			{
				errorMsg = "Movies that begin with a savestate are not supported.";
				return null;
			}

			//0x15
			Byte controllerflags = r.ReadByte();

			int numControllers;                   //number of controllers changes for SGB

			if ((controllerflags & 0x08) > 0) numControllers = 4;
			else if ((controllerflags & 0x04) > 0) numControllers = 3;
			else if ((controllerflags & 0x02) > 0) numControllers = 2;
			else numControllers = 1;

			//0x16
			Byte systemflags = r.ReadByte();    //what system is it?

			bool is_gba = false;
			bool is_gbc = false;
			bool is_sgb = false;
			bool is_gb = false;

			if ((systemflags & 0x04) > 0) is_sgb = true;
			if ((systemflags & 0x02) > 0) is_gbc = true;
			if ((systemflags & 0x01) > 0) is_gba = true;
			else is_gb = true;

			if (is_gb & is_gbc & is_gba & is_sgb)
			{
				errorMsg = "Not a valid .VBM platform type.";
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

			if ((flags & 0x40) > 0) echoramfix = true;
			if ((flags & 0x20) > 0) gbchdma5fix = true;
			if ((flags & 0x10) > 0) lagreduction = true;
			if ((flags & 0x08) > 0)
			{
				errorMsg = "Invalid .VBM file.";
				return null;
			}
			if ((flags & 0x04) > 0) rtcenable = true;
			if ((flags & 0x02) > 0) skipbiosfile = true;
			if ((flags & 0x01) > 0) usebiosfile = true;

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

			uint framesleft = framecount;

			r.BaseStream.Position = controllerdataoffset;    //advances to controller data.

			int currentoffset = (int)controllerdataoffset;

			SimpleController controllers = new SimpleController();
			controllers.Type = new ControllerDefinition();
			controllers.Type.Name = "Gameboy Controller";
			string[] buttons = new string[8] {"A", "B", "Select", "Start", "Right", "Left", "Up", "Down"};

			for (int frame = 1; frame <= framecount; frame++)
			{
				ushort controllerstate = r.ReadUInt16();
				// TODO: reset, GBA buttons go here
				byte and = 0x1;
				for (int button = 0; button < buttons.Length; button++)
				{
					controllers["P1 " + buttons[button]] = ((int)(controllerstate & and) > 0);
					and <<= 1;
				}
			}
			MnemonicsGenerator mg = new MnemonicsGenerator();
			mg.SetSource(controllers);
			m.AppendFrame(mg.GetControllersAsMnemonic());
			return m;
		}
	}
}