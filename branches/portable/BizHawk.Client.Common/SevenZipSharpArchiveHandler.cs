using System;
using System.IO;
using System.Collections.Generic;

using BizHawk.Common;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// Implementation of IHawkFileArchiveHandler using SevenZipSharp pure managed code
	/// </summary>
	public class SevenZipSharpArchiveHandler : IHawkFileArchiveHandler
	{
		public void Dispose()
		{
			if (extractor != null)
			{
				extractor.Dispose();
				extractor = null;
			}
		}

		public bool CheckSignature(string fileName, out int offset, out bool isExecutable)
		{
			SevenZip.FileChecker.ThrowExceptions = false;
			return SevenZip.FileChecker.CheckSignature(fileName, out offset, out isExecutable) != SevenZip.InArchiveFormat.None;
		}

		public IHawkFileArchiveHandler Construct(string path)
		{
			SevenZipSharpArchiveHandler ret = new SevenZipSharpArchiveHandler();
			ret.Open(path);
			return ret;
		}

		void Open(string path)
		{
			extractor = new SevenZip.SevenZipExtractor(path);
		}

		SevenZip.SevenZipExtractor extractor;

		public List<HawkFileArchiveItem> Scan()
		{
			List<HawkFileArchiveItem> ret = new List<HawkFileArchiveItem>();
			for (int i = 0; i < extractor.ArchiveFileData.Count; i++)
			{
				var afd = extractor.ArchiveFileData[i];
				if (afd.IsDirectory) continue;
				var ai = new HawkFileArchiveItem { name = HawkFile.Util_FixArchiveFilename(afd.FileName), size = (long)afd.Size, archiveIndex = i, index = ret.Count };
				ret.Add(ai);
			}

			return ret;
		}

		public void ExtractFile(int index, Stream stream)
		{
			extractor.ExtractFile(index, stream);
		}
	}

}