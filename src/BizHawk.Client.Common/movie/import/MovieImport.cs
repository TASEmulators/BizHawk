using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public static class MovieImport
	{
		private static readonly Dictionary<Type, ImporterForAttribute> Importers = Client.Common.ReflectionCache.Types
			.Select(t => (t, attr: (ImporterForAttribute) t.GetCustomAttributes(typeof(ImporterForAttribute)).FirstOrDefault()))
			.Where(tuple => tuple.attr != null)
			.ToDictionary(tuple => tuple.t, tuple => tuple.attr);

		/// <summary>
		/// Returns a value indicating whether or not there is an importer for the given extension
		/// </summary>
		public static bool IsValidMovieExtension(string extension)
		{
			return Importers
				.Select(i => i.Value)
				.Any(e => string.Equals(extension, e.Extension, StringComparison.OrdinalIgnoreCase));
		}

		public static readonly FilesystemFilterSet AvailableImporters = new FilesystemFilterSet(
			Importers.Values.OrderBy(attr => attr.Emulator)
				.Select(attr => new FilesystemFilter(attr.Emulator, new[] { attr.Extension.Substring(1) })) // substring removes initial '.'
				.ToArray()
		);

		// Attempt to import another type of movie file into a movie object.
		public static ImportResult ImportFile(IMovieSession session, IEmulator emulator, string path, Config config)
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

			return importer == null
				? ImportResult.Error($"No importer found for file type {ext}")
				: importer.Import(session, emulator, path, config);
		}

		private static Type ImporterForExtension(string ext)
		{
			return Importers.First(i => string.Equals(i.Value.Extension, ext, StringComparison.OrdinalIgnoreCase)).Key;
		}
	}
}
