#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Client.Common
{
	public sealed class FilesystemFilterSet(
		string? combinedEntryDesc,
		bool appendAllFilesEntry,
		params FilesystemFilter[] filters)
	{
		private const bool DEFAULT_USE_ALL_FILES_ENTRY = true;

		private readonly Lazy<string> _ser = new(() =>
			{
				var entries = filters.Select(static filter => filter.ToString()).ToList();
				if (combinedEntryDesc is not null) entries.Insert(0, FilesystemFilter.SerializeEntry(
					combinedEntryDesc,
					filters.SelectMany(static filter => filter.Extensions).Distinct().Order().ToList()));
				if (appendAllFilesEntry) entries.Add(FilesystemFilter.AllFilesEntry);
				return string.Join("|", entries);
			});

		public IReadOnlyList<FilesystemFilter> Filters
			=> filters;

		public FilesystemFilterSet(string combinedEntryDesc, params FilesystemFilter[] filters)
			: this(combinedEntryDesc, appendAllFilesEntry: DEFAULT_USE_ALL_FILES_ENTRY, filters) {}

		public FilesystemFilterSet(bool appendAllFilesEntry, params FilesystemFilter[] filters)
			: this(combinedEntryDesc: null, appendAllFilesEntry: appendAllFilesEntry, filters) {}

		public FilesystemFilterSet(params FilesystemFilter[] filters)
			: this(appendAllFilesEntry: DEFAULT_USE_ALL_FILES_ENTRY, filters) {}

		public override string ToString()
			=> _ser.Value;

		public static readonly FilesystemFilterSet Palettes = new(new FilesystemFilter("Palette Files", new[] { "pal" }));

		public static readonly FilesystemFilterSet Screenshots = new FilesystemFilterSet(FilesystemFilter.PNGs, new FilesystemFilter(".bmp Files", new[] { "bmp" }));
	}
}
