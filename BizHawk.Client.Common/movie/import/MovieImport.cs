using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;

namespace BizHawk.Client.Common
{
	public static class MovieImport
	{
		/// <summary>
		/// Returns a value indicating whether or not there is an importer for the given extension
		/// </summary>
		public static bool IsValidMovieExtension(string extension)
		{
			return SupportedExtensions.Any(e => string.Equals(extension, e, StringComparison.OrdinalIgnoreCase));
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

		private static readonly Dictionary<Type, string> Importers = Assembly.GetAssembly(typeof(ImportExtensionAttribute))
			.GetTypes()
			.Where(t => t.GetCustomAttributes(typeof(ImportExtensionAttribute))
				.Any())
			.ToDictionary(tkey => tkey, tvalue => ((ImportExtensionAttribute)tvalue.GetCustomAttributes(typeof(ImportExtensionAttribute))
				.First()).Extension);
			

		private static IEnumerable<string> SupportedExtensions => Importers
			.Select(i => i.Value)
			.ToList();
	}
}
