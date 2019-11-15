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
				.Any(e => string.Equals(extension, e.Extension, StringComparison.OrdinalIgnoreCase));
		}

		public static Dictionary<string, string> AvailableImporters()
		{
			return Importers
				.OrderBy(i => i.Value.Emulator)
				.ToDictionary(tkey => tkey.Value.Emulator, tvalue => tvalue.Value.Extension);
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
			IMovieImport importer = (IMovieImport)importerType
				.GetConstructor(new Type[] { })
				?.Invoke(new object[] { });

			if (importer == null)
			{
				return ImportResult.Error($"No importer found for file type {ext}");
			}

			return importer.Import(path);
		}

		private static Type ImporterForExtension(string ext)
		{
			return Importers.FirstOrDefault(i => string.Equals(i.Value.Extension, ext, StringComparison.OrdinalIgnoreCase)).Key;
		}

		private static readonly Dictionary<Type, ImportExtensionAttribute> Importers = Assembly.GetAssembly(typeof(ImportExtensionAttribute))
			.GetTypes()
			.Where(t => t.GetCustomAttributes(typeof(ImportExtensionAttribute))
				.Any())
			.ToDictionary(tkey => tkey, tvalue => ((ImportExtensionAttribute)tvalue.GetCustomAttributes(typeof(ImportExtensionAttribute))
				.First()));
	}
}
