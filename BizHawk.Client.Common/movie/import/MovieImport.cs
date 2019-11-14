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
			return Importers
				.Select(i => i.Value)
				.Any(e => string.Equals(extension, e, StringComparison.OrdinalIgnoreCase));
		}

		// Attempt to import another type of movie file into a movie object.
		public static ImportResult ImportFile(string path)
		{
			string ext = Path.GetExtension(path) ?? "";
			var importerType = ImporterForExtension(ext);

			if (importerType == default)
			{
				return ImportResult.Error($"No importer found for file type {ext}");
			}

			// Create a new instance of the importer class using the no-argument constructor
			IMovieImport importer = importerType
				.GetConstructor(new Type[] { })
				?.Invoke(new object[] { }) as IMovieImport;

			if (importer == null)
			{
				return ImportResult.Error($"No importer found for file type {ext}");
			}

			return importer.Import(path);
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
	}
}
