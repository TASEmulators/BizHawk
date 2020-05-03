#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Client.Common
{
	public sealed class FilesystemFilterSet
	{
		public readonly IReadOnlyCollection<FilesystemFilter> Filters;

		public FilesystemFilterSet(params FilesystemFilter[] filters)
		{
			Filters = filters;
		}

		/// <remarks>appends <c>All Files</c> entry (calls <see cref="ToString(bool)"/> with <see langword="true"/>), return value is a valid <c>Filter</c> for <c>Save-</c>/<c>OpenFileDialog</c></remarks>
		public override string ToString() => ToString(true);

		/// <remarks>call other overload to prepend combined entry, return value is a valid <c>Filter</c> for <c>Save-</c>/<c>OpenFileDialog</c></remarks>
		public string ToString(bool addAllFilesEntry) => addAllFilesEntry
			? $"{ToString(false)}|{FilesystemFilter.AllFilesEntry}"
			: string.Join("|", Filters.Select(filter => filter.ToString()));

		/// <remarks>call other overload (omit <paramref name="combinedEntryDesc"/>) to not prepend combined entry, return value is a valid <c>Filter</c> for <c>Save-</c>/<c>OpenFileDialog</c></remarks>
		public string ToString(string combinedEntryDesc, bool addAllFilesEntry = true)
			=> $"{FilesystemFilter.SerializeEntry(combinedEntryDesc, Filters.SelectMany(filter => filter.Extensions).Distinct().OrderBy(s => s))}|{ToString(addAllFilesEntry)}";

		public static readonly FilesystemFilterSet Screenshots = new FilesystemFilterSet(FilesystemFilter.PNGs, new FilesystemFilter(".bmp Files", new[] { "bmp" }));
	}
}
