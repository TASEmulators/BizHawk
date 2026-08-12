using System.Collections.Generic;
using System.IO;
using System.Linq;

using BizHawk.Common;

using LibArchive.Net;

namespace BizHawk.Client.Common
{
	/// <see cref="LibarchiveDearchivalMethod"/>
	public sealed class LibarchiveArchiveFile : IHawkArchiveFile
	{
		private LibArchiveReader? _handle;

		private FileInfo? _tempOnDisk;

		private (int ArchiveIndex, LibArchiveReader.Entry Entry)[]? field = null;

		private (int ArchiveIndex, LibArchiveReader.Entry Entry)[] AllEntries
			=> field ??= _handle?.Entries().Index().OrderBy(static tuple => tuple.Item.Name).ToArray()
				?? throw new ObjectDisposedException(nameof(LibarchiveArchiveFile));

		public LibarchiveArchiveFile(string path)
			=> _handle = new(path);

		public LibarchiveArchiveFile(Stream fileStream)
		{
			_tempOnDisk = new(TempFileManager.GetTempFilename("dearchive"));
			using (FileStream fsCopy = new(_tempOnDisk.FullName, FileMode.Create)) fileStream.CopyTo(fsCopy);
			try
			{
				_handle = new(_tempOnDisk.FullName);
			}
			catch (Exception e)
			{
				_tempOnDisk.Delete();
				Console.WriteLine(e);
				throw;
			}
		}

		public void Dispose()
		{
			_handle?.Dispose();
			_handle = null;
			_tempOnDisk?.Delete();
			_tempOnDisk = null;
		}

		public void ExtractFile(int index, Stream stream)
		{
			using var entryStream = AllEntries[index].Entry.Stream;
			entryStream.CopyTo(stream);
		}

		public List<HawkArchiveFileItem>? Scan()
			=> AllEntries.Select(static (tuple, i) => new HawkArchiveFileItem(
				tuple.Entry.Name,
				size: tuple.Entry.LengthBytes ?? 0,
				index: i,
				archiveIndex: tuple.ArchiveIndex)).ToList();
	}
}
