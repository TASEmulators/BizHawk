using System.Collections.Generic;
using System.IO;
using System.Linq;
using BizHawk.Common;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// Implementation of IHawkFileArchiveHandler using SharpCompress library
	/// Pure c# implementation for Mono (although this will work on Windows as well - but probably not as performant as SevenZipSharp)
	/// </summary>
	public class SharpCompressArchiveHandler : IHawkFileArchiveHandler
	{
		private SharpCompress.Archives.IArchive _archive;

		public void Dispose()
		{
			if (_archive != null)
			{
				_archive.Dispose();
				_archive = null;
			}
		}

		public bool CheckSignature(string fileName, out int offset, out bool isExecutable)
		{
			offset = 0;
			isExecutable = false;

			SharpCompress.Archives.IArchive arcTest = null; 

			try
			{
				arcTest = SharpCompress.Archives.ArchiveFactory.Open(fileName);
				var aType = arcTest.Type;

				switch(arcTest.Type)
				{
					case SharpCompress.Common.ArchiveType.Zip:
					case SharpCompress.Common.ArchiveType.SevenZip:
						return true;
				}
			}
			catch { }
			finally
			{
				if (arcTest != null)
					arcTest.Dispose();
			}

			return false;
		}

		public IHawkFileArchiveHandler Construct(string path)
		{
			var ret = new SharpCompressArchiveHandler();
			ret.Open(path);
			return ret;
		}

		private void Open(string path)
		{
			_archive = SharpCompress.Archives.ArchiveFactory.Open(path);
		}

		public List<HawkFileArchiveItem> Scan()
		{
			var ret = new List<HawkFileArchiveItem>();

			int idx = 0;
			foreach (var i in _archive.Entries)
			{
				if (i.IsDirectory)
				{
					continue;
				}

				var ai = new HawkFileArchiveItem
				{
					Name = HawkFile.Util_FixArchiveFilename(i.Key),
					Size = (long)i.Size,
					ArchiveIndex = idx++,
					Index = ret.Count
				};

				ret.Add(ai);
			}

			return ret;			
		}

		public void ExtractFile(int index, Stream stream)
		{
			int idx = 0;

			foreach (var i in _archive.Entries)
			{
				if (i.IsDirectory)
					continue;

				if (idx++ == index)
				{
					using (var entryStream = i.OpenEntryStream())
					{
						entryStream.CopyTo(stream);
						break;
					}
				}
			}
		}
	}
}