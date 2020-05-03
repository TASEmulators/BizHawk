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

		private IEnumerable<IArchiveEntry> ArchiveFiles => (_archive ?? throw new ObjectDisposedException(nameof(SharpCompressArchiveFile))).Entries.Where(e => !e.IsDirectory);

		public SharpCompressArchiveFile(string path) => _archive = ArchiveFactory.Open(path);

		public void Dispose()
		{
			_archive?.Dispose();
			_archive = null;
		}

		public void ExtractFile(int index, Stream stream)
		{
			using var entryStream = ArchiveFiles.ElementAt(index).OpenEntryStream();
			entryStream.CopyTo(stream);
		}

		public List<HawkArchiveFileItem> Scan() => ArchiveFiles.Select((e, i) => new HawkArchiveFileItem(e.Key.Replace('\\', '/'), e.Size, i, i)).ToList();
	}
}
