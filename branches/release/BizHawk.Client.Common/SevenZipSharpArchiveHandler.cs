using System.Collections.Generic;
using System.IO;

using BizHawk.Common;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// Implementation of IHawkFileArchiveHandler using SevenZipSharp pure managed code
	/// </summary>
	public class SevenZipSharpArchiveHandler : IHawkFileArchiveHandler
	{
		private SevenZip.SevenZipExtractor _extractor;

		public void Dispose()
		{
			if (_extractor != null)
			{
				_extractor.Dispose();
				_extractor = null;
			}
		}

		public bool CheckSignature(string fileName, out int offset, out bool isExecutable)
		{
			SevenZip.FileChecker.ThrowExceptions = false;
			return SevenZip.FileChecker.CheckSignature(fileName, out offset, out isExecutable) != SevenZip.InArchiveFormat.None;
		}

		public IHawkFileArchiveHandler Construct(string path)
		{
			var ret = new SevenZipSharpArchiveHandler();
			ret.Open(path);
			return ret;
		}

		private void Open(string path)
		{
			_extractor = new SevenZip.SevenZipExtractor(path);
		}

		public List<HawkFileArchiveItem> Scan()
		{
			var ret = new List<HawkFileArchiveItem>();
			for (int i = 0; i < _extractor.ArchiveFileData.Count; i++)
			{
				var afd = _extractor.ArchiveFileData[i];
				if (afd.IsDirectory)
				{
					continue;
				}

				var ai = new HawkFileArchiveItem
					{
						Name = HawkFile.Util_FixArchiveFilename(afd.FileName), 
						Size = (long)afd.Size, ArchiveIndex = i, Index = ret.Count
					};

				ret.Add(ai);
			}

			return ret;
		}

		public void ExtractFile(int index, Stream stream)
		{
			_extractor.ExtractFile(index, stream);
		}
	}
}