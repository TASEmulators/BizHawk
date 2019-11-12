using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using BizHawk.Common.BufferExtensions;
using BizHawk.Common.IOExtensions;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common.MovieConversionExtensions;

namespace BizHawk.Client.Common
{
	public static class MovieImport
	{
		// Movies 2.0 TODO: this is Movie.cs specific, can it be IMovie based? If not, needs to be refactored to a hardcoded 2.0 implementation, client needs to know what kind of type it imported to, or the mainform method needs to be moved here
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
				"BKM", "MMV"
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
	}
}
