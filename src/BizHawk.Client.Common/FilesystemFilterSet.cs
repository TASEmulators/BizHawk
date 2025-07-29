#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Client.Common
{
	public sealed class FilesystemFilterSet
	{
		private string? _ser = null;

		public bool AppendAllFilesEntry { get; set; } = true;

		public string? CombinedEntryDesc { get; set; } = null;

		private IReadOnlyCollection<string> CombinedExts
			=> Filters.SelectMany(static filter => filter.Extensions).Distinct().Order().ToList();

		public readonly IReadOnlyList<FilesystemFilter> Filters;

		public FilesystemFilterSet(params FilesystemFilter[] filters)
			=> Filters = filters;

		public override string ToString()
		{
			if (_ser is null)
			{
				var entries = Filters.Select(static filter => filter.ToString()).ToList();
				if (CombinedEntryDesc is not null) entries.Insert(0, FilesystemFilter.SerializeEntry(CombinedEntryDesc, CombinedExts));
				if (AppendAllFilesEntry) entries.Add(FilesystemFilter.AllFilesEntry);
				_ser = string.Join("|", entries);
			}
			return _ser;
		}

		public static readonly FilesystemFilterSet Palettes = new(new FilesystemFilter("Palette Files", new[] { "pal" }));

		public static readonly FilesystemFilterSet Screenshots = new FilesystemFilterSet(FilesystemFilter.PNGs, new FilesystemFilter(".bmp Files", new[] { "bmp" }));
	}
}
