using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using BizHawk.Common;

using SharpCompress.Archives;
using SharpCompress.Common;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// An <see cref="IHawkFileArchiveHandler">ArchiveHandler</see> implemented using SharpCompress from NuGet
	/// </summary>
	/// <remarks>
	/// Intended for Unix, which can't use SevenZipSharp, but later we might sacrifice whatever speed advantage that library has for the lower workload of one cross-platform library.
	/// </remarks>
	/// <seealso cref="SevenZipSharpArchiveHandler"/>
	public class SharpCompressArchiveHandler : IHawkFileArchiveHandler
	{
		private IArchive _archive;

		public void Dispose()
		{
			_archive?.Dispose();
			_archive = null;
		}

		/// <summary>
		/// whitelist extensions, to avoid thrown exceptions
		/// </summary>
		public string[] ArchiveExtensions = { ".zip", ".gz", ".gzip", ".tar", ".rar", ".7z" };

		public bool CheckSignature(string fileName, out int offset, out bool isExecutable)
		{
			offset = 0;
			isExecutable = false;
			
			var pathExt = Path.GetExtension(fileName)?.ToLower();
			if (!ArchiveExtensions.Contains(pathExt))
				return false;

			try
			{
				using var arcTest = ArchiveFactory.Open(fileName);
				switch (arcTest.Type)
				{
					case ArchiveType.Zip:
					case ArchiveType.SevenZip:
						return true;
				}
			}
			catch (Exception)
			{
				// ignored
			}
			return false;
		}

		public IHawkFileArchiveHandler Construct(string path)
		{
			var ret = new SharpCompressArchiveHandler();
			ret.Open(path);
			return ret;
		}

		private void Open(string path) => _archive = ArchiveFactory.Open(path);

		public List<HawkFileArchiveItem> Scan() =>
			_archive.Entries.Where(e => !e.IsDirectory)
				.Select((e, i) => new HawkFileArchiveItem
				{
					Name = HawkFile.Util_FixArchiveFilename(e.Key),
					Size = e.Size,
					Index = i,
					ArchiveIndex = i
				})
				.ToList();

		public void ExtractFile(int index, Stream stream)
		{
			using var entryStream = _archive.Entries.Where(e => !e.IsDirectory).ElementAt(index).OpenEntryStream();
			entryStream.CopyTo(stream);
		}
	}
}