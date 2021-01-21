#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using BizHawk.Common;

using SharpCompress.Archives;

namespace BizHawk.Client.Common
{
	/// <see cref="SharpCompressDearchivalMethod"/>
	public class SharpCompressArchiveFile : IHawkArchiveFile
	{
		private IArchive? _archive;

		private IEnumerable<IArchiveEntry> EnumerateArchiveFiles()
		{
			if (_archive == null) throw new ObjectDisposedException(nameof(SharpCompressArchiveFile));
			return _archive.Entries.Where(e => !e.IsDirectory);
		}

		public SharpCompressArchiveFile(string path) => _archive = ArchiveFactory.Open(path, new());

		public void Dispose()
		{
			if (_archive == null) throw new ObjectDisposedException(nameof(SharpCompressArchiveFile));
			_archive.Dispose();
			_archive = null;
		}

		public void ExtractFile(int index, Stream stream)
		{
			var reader = _archive!.ExtractAllEntries();
			for (var i = 0; i <= index; i++) reader.MoveToNextEntry();
			using var entryStream = reader.OpenEntryStream();
			entryStream.CopyTo(stream);
		}

		public List<HawkArchiveFileItem>? Scan()
		{
			List<HawkArchiveFileItem> outFiles = new();
			var entries = EnumerateArchiveFiles().ToList();
			for (var i = 0; i < entries.Count; i++)
			{
				var entry = entries[i];
				if (entry.Key == null) return null;
				outFiles.Add(new HawkArchiveFileItem(entry.Key.Replace('\\', '/'), entry.Size, i, i));
			}
			return outFiles;
		}
	}
}
