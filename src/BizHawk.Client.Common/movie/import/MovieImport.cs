using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;

using BizHawk.Common.CollectionExtensions;

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
				.ToArray())
			{
				CombinedEntryDesc = "Movie Files",
			};

		// Attempt to import another type of movie file into a movie object.
		public static ImportResult ImportFile(
			IDialogParent dialogParent,
			IMovieSession session,
			string path,
			Config config)
		{
			string ext = Path.GetExtension(path) ?? "";
			var result = Importers.FirstOrNull(kvp => string.Equals(kvp.Value.Extension, ext, StringComparison.OrdinalIgnoreCase));
			// Create a new instance of the importer class using the no-argument constructor
			return result is { Key: var importerType }
				&& importerType.GetConstructor(Type.EmptyTypes)?.Invoke(Array.Empty<object>()) is IMovieImport importer
					? importer.Import(dialogParent, session, path, config)
					: ImportResult.Error($"No importer found for file type {ext}");
		}
	}
}
