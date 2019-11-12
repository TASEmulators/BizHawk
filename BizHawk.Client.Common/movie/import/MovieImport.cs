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
				"BKM"
			};
			return extensions.Any(ext => extension.ToUpper() == $".{ext}");
		}
	}
}
