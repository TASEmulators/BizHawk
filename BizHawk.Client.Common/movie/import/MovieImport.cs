using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using BizHawk.Common.BufferExtensions;
using BizHawk.Common.IOExtensions;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common.MovieConversionExtensions;

using BizHawk.Emulation.Cores.Nintendo.SNES9X;

namespace BizHawk.Client.Common
{
	public static class MovieImport
	{
		// Movies 2.0 TODO: this is Movie.cs specific, can it be IMovie based? If not, needs to be refactored to a hardcoded 2.0 implementation, client needs to know what kind of type it imported to, or the mainform method needs to be moved here
		private const string CRC32 = "CRC32";
		private const string EMULATIONORIGIN = "emuOrigin";
		private const string JAPAN = "Japan";
		private const string MD5 = "MD5";
		private const string MOVIEORIGIN = "MovieOrigin";

		/// <summary>
		/// Returns a value indicating whether or not there is an importer for the given extension
		/// </summary>
		public static bool IsValidMovieExtension(string extension)
		{
			return SupportedExtensions.Any(e => string.Equals(extension, e, StringComparison.OrdinalIgnoreCase))
				|| UsesLegacyImporter(extension);
		}

		/// <summary>
		/// Attempts to convert a movie with the given filename to a support
		/// <seealso cref="IMovie"/> type
		/// </summary>
		/// <param name="fn">The path to the file to import</param>
		/// <param name="conversionErrorCallback">The callback that will be called if an error occurs</param>
		/// <param name="messageCallback">The callback that will be called if any messages need to be presented to the user</param>
		public static void Import(string fn, Action<string> conversionErrorCallback, Action<string> messageCallback)
		{
			var d = PathManager.MakeAbsolutePath(Global.Config.PathEntries.MoviesPathFragment, null);
			var m = ImportFile(fn, out var errorMsg, out var warningMsg);

			if (!string.IsNullOrWhiteSpace(errorMsg))
			{
				conversionErrorCallback(errorMsg);
			}

			messageCallback(!string.IsNullOrWhiteSpace(warningMsg)
				? warningMsg
				: $"{Path.GetFileName(fn)} imported as {m.Filename}");

			if (!Directory.Exists(d))
			{
				Directory.CreateDirectory(d);
			}
		}

		// Attempt to import another type of movie file into a movie object.
		public static IMovie ImportFile(string path, out string errorMsg, out string warningMsg)
		{
			errorMsg = "";
			warningMsg = "";
			string ext = path != null ? Path.GetExtension(path).ToUpper() : "";

			if (UsesLegacyImporter(ext))
			{
				return LegacyImportFile(ext, path, out errorMsg, out warningMsg).ToBk2();
			}

			var importerType = ImporterForExtension(ext);

			if (importerType == default)
			{
				errorMsg = $"No importer found for file type {ext}";
				return null;
			}

			// Create a new instance of the importer class using the no-argument constructor
			IMovieImport importer = importerType
				.GetConstructor(new Type[] { })
				?.Invoke(new object[] { }) as IMovieImport;

			if (importer == null)
			{
				errorMsg = $"No importer found for type {ext}";
				return null;
			}

			Bk2Movie movie = null;

			try
			{
				var result = importer.Import(path);
				if (result.Errors.Count > 0)
				{
					errorMsg = result.Errors.First();
				}

				if (result.Warnings.Count > 0)
				{
					warningMsg = result.Warnings.First();
				}

				movie = result.Movie;
			}
			catch (Exception ex)
			{
				errorMsg = ex.ToString();
			}

			movie?.Save();
			return movie;
		}

		private static Type ImporterForExtension(string ext)
		{
			return Importers.FirstOrDefault(i => string.Equals(i.Value, ext, StringComparison.OrdinalIgnoreCase)).Key;
		}

		private static BkmMovie LegacyImportFile(string ext, string path, out string errorMsg, out string warningMsg)
		{
			errorMsg = "";
			warningMsg = "";

			BkmMovie m = new BkmMovie();

			try
			{
				switch (ext)
				{
					case ".MMV":
						m = ImportMmv(path, out errorMsg, out warningMsg);
						break;
					case ".SMV":
						m = ImportSmv(path, out errorMsg, out warningMsg);
						break;
					case ".BKM":
						m.Filename = path;
						m.Load(false);
						break;
				}
			}
			catch (Exception except)
			{
				errorMsg = except.ToString();
			}

			if (m != null)
			{
				m.Filename += $".{BkmMovie.Extension}";
			}
			else
			{
				throw new Exception(errorMsg);
			}
			
			return m;
		}

		private static readonly Dictionary<Type, string> Importers = Assembly.GetAssembly(typeof(ImportExtensionAttribute))
			.GetTypes()
			.Where(t => t.GetCustomAttributes(typeof(ImportExtensionAttribute))
				.Any())
			.ToDictionary(tkey => tkey, tvalue => ((ImportExtensionAttribute)tvalue.GetCustomAttributes(typeof(ImportExtensionAttribute))
				.First()).Extension);
			

		private static IEnumerable<string> SupportedExtensions => Importers
			.Select(i => i.Value)
			.ToList();

		// Return whether or not the type of file provided is currently imported by a legacy (i.e. to BKM not BK2) importer
		private static bool UsesLegacyImporter(string extension)
		{
			string[] extensions =
			{
				"BKM", "MMV", "SMV"
			};
			return extensions.Any(ext => extension.ToUpper() == $".{ext}");
		}

		// Ends the string where a NULL character is found.
		private static string NullTerminated(string str)
		{
			int pos = str.IndexOf('\0');
			if (pos != -1)
			{
				str = str.Substring(0, pos);
			}

			return str;
		}

		// MMV file format: http://tasvideos.org/MMV.html
		private static BkmMovie ImportMmv(string path, out string errorMsg, out string warningMsg)
		{
			errorMsg = warningMsg = "";
			BkmMovie m = new BkmMovie(path);
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
			m.Comments.Add($"{EMULATIONORIGIN} Dega version {emuVersion}");
			m.Comments.Add($"{MOVIEORIGIN} .MMV");

			// 0008: 4-byte little endian unsigned int: frame count
			uint frameCount = r.ReadUInt32();

			// 000c: 4-byte little endian unsigned int: rerecord count
			uint rerecordCount = r.ReadUInt32();
			m.Rerecords = rerecordCount;

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
			m.Header[HeaderKeys.AUTHOR] = author;

			// 0060: 4-byte little endian flags
			byte flags = r.ReadByte();

			// bit 0: unused
			// bit 1: "PAL"
			bool pal = ((flags >> 1) & 0x1) != 0;
			m.Header[HeaderKeys.PAL] = pal.ToString();

			// bit 2: Japan
			bool japan = ((flags >> 2) & 0x1) != 0;
			m.Header[JAPAN] = japan.ToString();

			// bit 3: Game Gear (version 1.16+)
			bool gamegear;
			if (((flags >> 3) & 0x1) != 0)
			{
				gamegear = true;
				m.Header[HeaderKeys.PLATFORM] = "GG";
			}
			else
			{
				gamegear = false;
				m.Header[HeaderKeys.PLATFORM] = "SMS";
			}

			// bits 4-31: unused
			r.ReadBytes(3);

			// 0064-00e3: string: rom name (ASCII)
			string gameName = NullTerminated(r.ReadStringFixedAscii(128));
			m.Header[HeaderKeys.GAMENAME] = gameName;

			// 00e4-00f3: binary: rom MD5 digest
			byte[] md5 = r.ReadBytes(16);
			m.Header[MD5] = $"{md5.BytesToHexString().ToLower():x8}";
			var controllers = new SimpleController { Definition = new ControllerDefinition { Name = "SMS Controller" } };

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
							(((controllerState >> 6) & 0x1) != 0 && (!gamegear))
							|| (((controllerState >> 7) & 0x1) != 0 && gamegear);
					}
				}

				m.AppendFrame(controllers);
			}

			r.Close();
			fs.Close();
			return m;
		}

		private static BkmMovie ImportSmv(string path, out string errorMsg, out string warningMsg)
		{
			errorMsg = warningMsg = "";
			BkmMovie m = new BkmMovie(path);
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

			m.Header[HeaderKeys.PLATFORM] = "SNES";

			// 004 4-byte little-endian unsigned int: version number
			uint versionNumber = r.ReadUInt32();
			string version;
			switch (versionNumber)
			{
				case 1:
					version = "1.43";
					break;
				case 4:
					version = "1.51";
					break;
				case 5:
					version = "1.52";
					break;
				default:
					errorMsg = "SMV version not recognized. 1.43, 1.51, and 1.52 are currently supported.";
					r.Close();
					fs.Close();
					return null;
			}

			m.Comments.Add($"{EMULATIONORIGIN} Snes9x version {version}");
			m.Comments.Add($"{MOVIEORIGIN} .SMV");
			/*
			 008 4-byte little-endian integer: movie "uid" - identifies the movie-savestate relationship, also used as the
			 recording time in Unix epoch format
			*/
			uint uid = r.ReadUInt32();

			// 00C 4-byte little-endian unsigned int: rerecord count
			m.Rerecords = r.ReadUInt32();

			// 010 4-byte little-endian unsigned int: number of frames
			uint frameCount = r.ReadUInt32();

			// 014 1-byte flags "controller mask"
			byte controllerFlags = r.ReadByte();
			/*
			 * bit 0: controller 1 in use
			 * bit 1: controller 2 in use
			 * bit 2: controller 3 in use
			 * bit 3: controller 4 in use
			 * bit 4: controller 5 in use
			 * other: reserved, set to 0
			*/
			SimpleController controllers = new SimpleController { Definition = new ControllerDefinition { Name = "SNES Controller" } };
			bool[] controllersUsed = new bool[5];
			for (int controller = 1; controller <= controllersUsed.Length; controller++)
			{
				controllersUsed[controller - 1] = ((controllerFlags >> (controller - 1)) & 0x1) != 0;
			}

			// 015 1-byte flags "movie options"
			byte movieFlags = r.ReadByte();
			/*
				 bit 0:
					 if "0", movie begins from an embedded "quicksave" snapshot
					 if "1", a SRAM is included instead of a quicksave; movie begins from reset
			*/
			if ((movieFlags & 0x1) == 0)
			{
				errorMsg = "Movies that begin with a savestate are not supported.";
				r.Close();
				fs.Close();
				return null;
			}

			// bit 1: if "0", movie is NTSC (60 fps); if "1", movie is PAL (50 fps)
			bool pal = ((movieFlags >> 1) & 0x1) != 0;
			m.Header[HeaderKeys.PAL] = pal.ToString();

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
			byte syncFlags = r.ReadByte();
			/*
			 Extra ROM info is always positioned right before the savestate. Its size is 30 bytes if MOVIE_SYNC_HASROMINFO
			 is used (and MOVIE_SYNC_DATA_EXISTS is set), 0 bytes otherwise.
			*/
			int extraRomInfo = (((syncFlags >> 6) & 0x1) != 0 && (syncFlags & 0x1) != 0) ? 30 : 0;

			// 018 4-byte little-endian unsigned int: offset to the savestate inside file
			uint savestateOffset = r.ReadUInt32();

			// 01C 4-byte little-endian unsigned int: offset to the controller data inside file
			uint firstFrameOffset = r.ReadUInt32();
			int[] controllerTypes = new int[2];

			// The (.SMV 1.51 and up) header has an additional 32 bytes at the end
			if (version != "1.43")
			{
				// 020 4-byte little-endian unsigned int: number of input samples, primarily for peripheral-using games
				r.ReadBytes(4);
				/*
				 024 2 1-byte unsigned ints: what type of controller is plugged into ports 1 and 2 respectively: 0=NONE,
				 1=JOYPAD, 2=MOUSE, 3=SUPERSCOPE, 4=JUSTIFIER, 5=MULTITAP
				*/
				controllerTypes[0] = r.ReadByte();
				controllerTypes[1] = r.ReadByte();

				// 026 4 1-byte signed ints: controller IDs of port 1, or -1 for unplugged
				r.ReadBytes(4);

				// 02A 4 1-byte signed ints: controller IDs of port 2, or -1 for unplugged
				r.ReadBytes(4);

				// 02E 18 bytes: reserved for future use
				r.ReadBytes(18);
			}

			/*
			 After the header comes "metadata", which is UTF16-coded movie title string (author info). The metadata begins
			 from position 32 (0x20 (0x40 for 1.51 and up)) and ends at <savestate_offset -
			 length_of_extra_rom_info_in_bytes>.
			*/
			byte[] metadata = r.ReadBytes((int)(savestateOffset - extraRomInfo - ((version != "1.43") ? 0x40 : 0x20)));
			string author = NullTerminated(Encoding.Unicode.GetString(metadata).Trim());
			if (author != "")
			{
				m.Header[HeaderKeys.AUTHOR] = author;
			}

			if (extraRomInfo == 30)
			{
				// 000 3 bytes of zero padding: 00 00 00 003 4-byte integer: CRC32 of the ROM 007 23-byte ascii string
				r.ReadBytes(3);
				int crc32 = r.ReadInt32();
				m.Header[CRC32] = crc32.ToString();

				// the game name copied from the ROM, truncated to 23 bytes (the game name in the ROM is 21 bytes)
				string gameName = NullTerminated(Encoding.UTF8.GetString(r.ReadBytes(23)));
				m.Header[HeaderKeys.GAMENAME] = gameName;
			}

			r.BaseStream.Position = firstFrameOffset;
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
			string[] buttons =
			{
				"Right", "Left", "Down", "Up", "Start", "Select", "Y", "B", "R", "L", "X", "A"
			};

			for (int frame = 0; frame <= frameCount; frame++)
			{
				controllers["Reset"] = true;
				for (int player = 1; player <= controllersUsed.Length; player++)
				{
					if (!controllersUsed[player - 1])
					{
						continue;
					}

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
					{
						controllers["Reset"] = false;
					}

					/*
					 While the meaning of controller data (for 1.51 and up) for a single standard SNES controller pad
					 remains the same, each frame of controller data can contain additional bytes if input for peripherals
					 is being recorded.
					*/
					if (version != "1.43" && player <= controllerTypes.Length)
					{
						var peripheral = "";
						switch (controllerTypes[player - 1])
						{
							case 0: // NONE
								continue;
							case 1: // JOYPAD
								break;
							case 2: // MOUSE
								peripheral = "Mouse";

								// 5*num_mouse_ports
								r.ReadBytes(5);
								break;
							case 3: // SUPERSCOPE
								peripheral = "Super Scope"; // 6*num_superscope_ports
								r.ReadBytes(6);
								break;
							case 4: // JUSTIFIER
								peripheral = "Justifier";

								// 11*num_justifier_ports
								r.ReadBytes(11);
								break;
							case 5: // MULTITAP
								peripheral = "Multitap";
								break;
						}

						if (peripheral != "" && warningMsg == "")
						{
							warningMsg = $"Unable to import {peripheral}.";
						}
					}

					ushort controllerState = (ushort)(((controllerState1 << 4) & 0x0F00) | controllerState2);
					if (player <= BkmMnemonicConstants.Players[controllers.Definition.Name])
					{
						for (int button = 0; button < buttons.Length; button++)
						{
							controllers[$"P{player} {buttons[button]}"] =
								((controllerState >> button) & 0x1) != 0;
						}
					}
					else if (warningMsg == "")
					{
						warningMsg = $"Controller {player} not supported.";
					}
				}

				// The controller data contains <number_of_frames + 1> frames.
				if (frame == 0)
				{
					continue;
				}

				m.AppendFrame(controllers);
			}

			r.Close();
			fs.Close();

			var ss = new Snes9x.SyncSettings();
			m.SyncSettingsJson = ConfigService.SaveWithType(ss);
			Global.Config.SNES_InSnes9x = true; // This could be annoying to a user if they don't notice we set this preference, but the alternative is for the movie import to fail to load the movie
			return m;
		}
	}
}
