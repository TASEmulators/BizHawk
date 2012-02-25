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
				m.Header.SetHeaderLine(MovieHeader.MOVIEVERSION, MovieHeader.MovieVersion);
				if (errorMsg == "")
				{
					m.WriteMovie();
				}
			}
			catch (Exception except)
			{
				errorMsg = except.ToString();
			}
			return m;
		}

		public static bool IsValidMovieExtension(string extension)
		{
			string[] extensions = new string[9] { "FCM", "FM2", "FMV", "GMV", "MC2", "MMV", "SMV", "TAS", "VBM" };
			foreach (string ext in extensions)
			{
				if (extension.ToUpper() == "." + ext)
				{
					return true;
				}
			}
			return false;
		}

		private static string ParseHeader(string line, string headerName)
		{
			string str;
			int x = line.LastIndexOf(headerName) + headerName.Length;
			str = line.Substring(x + 1, line.Length - x - 1);
			return str;
		}

		private static bool AddSubtitle(ref Movie m, string subtitleStr)
		{
			if (subtitleStr.Length == 0)
				return false;
			Subtitle s = new Subtitle();
			int x = subtitleStr.IndexOf(' ');
			if (x <= 0)
				return false;
			// Remove the "subtitle" header from the string.
			string sub = subtitleStr.Substring(x + 1, subtitleStr.Length - x - 1);
			x = sub.IndexOf(' ');
			if (x <= 0)
				return false;
			// The frame and message are separated by a space.
			string frame = sub.Substring(0, x);
			string message = sub.Substring(x + 1, sub.Length - x - 1);
			m.Subtitles.AddSubtitle("subtitle " + frame + " 0 0 200 FFFFFFFF " + message);
			return true;
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
				int line = 0;
				string str = "";
				string rerecordStr = "";
				while ((str = sr.ReadLine()) != null)
				{
					line++;
					if (str == "")
					{
						continue;
					}
					if (str.Contains("rerecordCount"))
					{
						rerecordStr = ParseHeader(str, "rerecordCount");
						// Try to parse the Re-record count as an integer, defaulting to 0 if it fails.
						try
						{
							m.SetRerecords(int.Parse(rerecordStr));
						}
						catch
						{
							m.SetRerecords(0);
						}
					}
					else if (str.Contains("StartsFromSavestate"))
					{
						str = ParseHeader(str, "StartsFromSavestate");
						// If this movie starts from a savestate, we can't support it.
						if (str == "1")
						{
							warningMsg = "Movies that begin with a savestate are not supported.";
							return null;
						}
					}
					if (str.StartsWith("emuVersion"))
					{
						m.Header.Comments.Add("emuOrigin " + emulator + " version " + ParseHeader(str, "emuVersion"));
					}
					else if (str.StartsWith("version"))
					{
						m.Header.Comments.Add(
							"MovieOrigin " + Path.GetExtension(path) + " version " + ParseHeader(str, "version")
						);
					}
					else if (str.StartsWith("romFilename"))
					{
						m.Header.SetHeaderLine(MovieHeader.GAMENAME, ParseHeader(str, "romFilename"));
					}
					else if (str.StartsWith("comment author"))
					{
						m.Header.SetHeaderLine(MovieHeader.AUTHOR, ParseHeader(str, "comment author"));
					}
					else if (str.StartsWith("guid"))
					{
						m.Header.SetHeaderLine(MovieHeader.GUID, ParseHeader(str, "GUID"));
					}
					else if (str.StartsWith("subtitle") || str.StartsWith("sub"))
					{
						AddSubtitle(ref m, str);
					}
					else if (str[0] == '|')
					{
						// Handle a frame of input.
						ArrayList frame = new ArrayList();
						// Split up the sections of the frame.
						string[] sections = str.Split('|');
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
							{
								warningMsg = "Unable to import " + warningMsg + " command on line " + line;
							}
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
							{
								for (int button = 0; button < buttons.Length; button++)
								{
									// Consider the button pressed so long as its spot is not occupied by a ".".
									controllers["P" + (player).ToString() + " " + buttons[button]] = (
										sections[section][button] != '.'
									);
								}
							}
						}
						// Convert the data for the controllers to a mnemonic and add it as a frame.
						MnemonicsGenerator mg = new MnemonicsGenerator();
						mg.SetSource(controllers);
						m.AppendFrame(mg.GetControllersAsMnemonic());
					}
					else
					{
						// Everything not explicitly defined is treated as a comment.
						m.Header.Comments.Add(str);
					}
				}
			}
			return m;
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
				errorMsg = "This is not a valid FCM file!";
				return null;
			}

			UInt32 version = r.ReadUInt32();
			m.Header.SetHeaderLine(MovieHeader.MovieVersion, "FCEU movie version " + version.ToString() + " (.fcm)");

			byte[] flags = new byte[4];
			for (int x = 0; x < 4; x++)
				flags[x] = r.ReadByte();

			UInt32 frameCount = r.ReadUInt32();

			m.SetRerecords((int)r.ReadUInt32());

			UInt32 movieDataSize = r.ReadUInt32();
			UInt32 savestateOffset = r.ReadUInt32();
			UInt32 firstFrameOffset = r.ReadUInt32();

			byte[] romCheckSum = r.ReadBytes(16);
			//TODO: ROM checksum movie header line (MD5)

			UInt32 EmuVersion = r.ReadUInt32();
			m.Header.Comments.Add("emuOrigin FCEU " + EmuVersion.ToString());

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

			byte[] signatureBytes = new byte[4];
			for (int x = 0; x < 4; x++)
				signatureBytes[x] = r.ReadByte();
			string signature = System.Text.Encoding.UTF8.GetString(signatureBytes);
			if (signature != "MMV\0")
			{
				errorMsg = "This is not a valid MMV file.";
				return null;
			}

			UInt32 version = r.ReadUInt32();
			m.Header.Comments.Add("MovieOrigin .mmv version " + version.ToString());
			UInt32 framecount = r.ReadUInt32();

			m.SetRerecords((int)r.ReadUInt32());

			UInt32 IsFromReset = r.ReadUInt32();
			if (IsFromReset == 0)
			{
				errorMsg = "Movies that begin with a savestate are not supported.";
				return null;
			}

			UInt32 stateOffset = r.ReadUInt32();
			UInt32 inputDataOffset = r.ReadUInt32();
			UInt32 inputPacketSize = r.ReadUInt32();

			byte[] authorBytes = new byte[64];
			for (int x = 0; x < 64; x++)
				authorBytes[x] = r.ReadByte();

			string author = System.Text.Encoding.UTF8.GetString(authorBytes);
			//TODO: remove null characters
			m.Header.SetHeaderLine(MovieHeader.AUTHOR, author);

			//4-byte little endian flags
			byte flags = r.ReadByte();

			bool pal;
			if ((int)(flags & 2) > 0)
				pal = true;
			else
				pal = false;
			m.Header.SetHeaderLine("PAL", pal.ToString());

			bool japan;
			if ((int)(flags & 4) > 0)
				japan = true;
			else
				japan = false;
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

			r.ReadBytes(3); //Unused flags

			byte[] romnameBytes = new byte[128];
			for (int x = 0; x < 128; x++)
				romnameBytes[x] = r.ReadByte();
			string romname = System.Text.Encoding.UTF8.GetString(romnameBytes.ToArray());
			//TODO: remove null characters
			m.Header.SetHeaderLine(MovieHeader.GAMENAME, romname);

			byte[] MD5Bytes = new byte[16];
			for (int x = 0; x < 16; x++)
				MD5Bytes[x] = r.ReadByte();
			string MD5 = System.Text.Encoding.UTF8.GetString(MD5Bytes.ToArray());
			//TODO: format correctly
			m.Header.SetHeaderLine("MD5", MD5);

			for (int x = 0; x < (framecount); x++)
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
					{
						controllers["Pause"] = (
							((int)(controllerstate & 0x40) > 0 && (!gamegear)) ||
							((int)(controllerstate & 0x80) > 0 && gamegear)
						);
					}
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
				errorMsg = "This is not a valid SMV file.";
				return null;
			}

			UInt32 version = r.ReadUInt32();

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
					errorMsg = "SMV version not recognized, 143, 151, and 152 are currently supported";
					return null;
				}
			}
		}

		// SMV 1.43 file format: http://code.google.com/p/snes9x-rr/wiki/SMV143
		private static Movie ImportSMV143(BinaryReader r, string path)
		{
			Movie m = new Movie(Path.ChangeExtension(path, ".tas"), MOVIEMODE.PLAY);

			UInt32 GUID = r.ReadUInt32();
			m.Header.SetHeaderLine(MovieHeader.GUID, GUID.ToString()); //TODO: format to hex string
			m.SetRerecords((int)r.ReadUInt32());

			UInt32 framecount = r.ReadUInt32();
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

			UInt32 SavestateOffset = r.ReadUInt32();
			UInt32 FrameDataOffset = r.ReadUInt32();

			//TODO: get extra rom info

			r.BaseStream.Position = FrameDataOffset;
			for (int x = 0; x < framecount; x++)
			{
				//TODO: FF FF for all controllers = Reset
				//string frame = "|0|";
				for (int y = 0; y < numControllers; y++)
				{
					UInt16 fd = r.ReadUInt16();
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
			UInt32 GUID = r.ReadUInt32();
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
			UInt32 signature = r.ReadUInt32();  //always 56 42 4D 1A  (VBM\x1A)
			if (signature != 0x56424D1A)
			{
				errorMsg = "This is not a valid VBM file.";
				return null;
			}

			UInt32 versionno = r.ReadUInt32();  //always 1
			UInt32 uid = r.ReadUInt32();		//time of recording
			m.Header.SetHeaderLine(MovieHeader.GUID, uid.ToString());
			UInt32 framecount = r.ReadUInt32();

			//0x10
			UInt32 rerecordcount = r.ReadUInt32();
			m.SetRerecords((int)rerecordcount);
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
				errorMsg = "Not a valid VBM platform type.";
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
				errorMsg = "Invalid VBM file";
				return null;
			}
			if ((flags & 0x04) > 0) rtcenable = true;
			if ((flags & 0x02) > 0) skipbiosfile = true;
			if ((flags & 0x01) > 0) usebiosfile = true;

			//0x18
			UInt32 winsavetype = r.ReadUInt32();
			UInt32 winflashsize = r.ReadUInt32();

			//0x20
			UInt32 gbemulatortype = r.ReadUInt32();

			char[] internalgamename = r.ReadChars(0x0C);
			string gamename = new String(internalgamename);
			m.Header.SetHeaderLine(MovieHeader.GAMENAME, gamename);

			//0x30
			Byte minorversion = r.ReadByte();
			Byte internalcrc = r.ReadByte();
			UInt16 internalchacksum = r.ReadUInt16();
			UInt32 unitcode = r.ReadUInt32();
			UInt32 saveoffset = r.ReadUInt32();		//set to 0 if unused
			UInt32 controllerdataoffset = r.ReadUInt32();

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

			UInt32 framesleft = framecount;

			r.BaseStream.Position = controllerdataoffset;    //advances to controller data.

			int currentoffset = (int)controllerdataoffset;

			SimpleController controllers = new SimpleController();
			controllers.Type = new ControllerDefinition();
			controllers.Type.Name = "Gameboy Controller";
			string[] buttons = new string[8] {"A", "B", "Select", "Start", "Right", "Left", "Up", "Down"};

			for (int frame = 1; frame <= framecount; frame++)
			{
				UInt16 controllerstate = r.ReadUInt16();
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