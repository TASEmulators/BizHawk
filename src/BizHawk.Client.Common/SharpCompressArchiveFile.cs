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
		private IArchive _archive;

		private IEnumerable<IArchiveEntry> EnumerateArchiveFiles()
		{
			if (_archive == null)
				throw new ObjectDisposedException(nameof(SharpCompressArchiveFile));
			return _archive.Entries.Where(e => !e.IsDirectory);
		}

		public SharpCompressArchiveFile(string path)
		{
			var readerOptions = new SharpCompress.Readers.ReaderOptions();
			_archive = ArchiveFactory.Open(path, readerOptions);
		}

		public void Dispose()
		{
			_archive.Dispose();
			_archive = null;
		}

		public void ExtractFile(int index, Stream stream)
		{
			var reader = _archive.ExtractAllEntries();
			for(int i=0;i<=index;i++)
				reader.MoveToNextEntry();

			using var entryStream = reader.OpenEntryStream();
			entryStream.CopyTo(stream);
		}

		public List<HawkArchiveFileItem> Scan()
		{
			var files = EnumerateArchiveFiles();
			return files.Select((e, i) => new HawkArchiveFileItem(e.Key.Replace('\\', '/'), e.Size, i, i)).ToList();
		}
	}
}
