using System;
using System.IO;
using System.Collections.Generic;
using SharpCompress.Archive;
using SharpCompress.Reader;

using BizHawk.Common;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// Implementation of IHawkFileArchiveHandler using SevenZipSharp pure managed code... Just kidding!
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
			if (stream != null) 
			{
				stream.Dispose ();
				stream = null;
			}
		}

		public bool CheckSignature(string fileName, out int offset, out bool isExecutable){
			offset = 0;
			isExecutable = false;
			//I don't think I can do this very well. I have no idea why isExecutable is needed, and SharpCompress doesn't provide offsets.

			using(FileStream fs = new FileStream (fileName, FileMode.Open))
			{
				try
				{
					IArchive chk = ArchiveFactory.Open(fs, SharpCompress.Common.Options.None);
				}
				catch(Exception ex)
				{
					return false;
				}
			}

			return true; //This is an archive
		}

		public IHawkFileArchiveHandler Construct(string path)
		{
			SevenZipSharpArchiveHandler ret = new SevenZipSharpArchiveHandler();
			ret.Open(path);
			return ret;
		}

		void Open(string path)
		{
			if (stream != null) {
				stream.Dispose();
				stream = null;
			}
			stream = new FileStream(path, FileMode.Open);
			extractor = ArchiveFactory.Open(stream, SharpCompress.Common.Options.None);
		}

		IArchive extractor;
		FileStream stream;

		public List<HawkFileArchiveItem> Scan()
		{
			List<HawkFileArchiveItem> ret = new List<HawkFileArchiveItem>();
			int i = -1;
			foreach(var afd in extractor.Entries)
			{
				i++;
				if (afd.IsDirectory) continue;
				var ai = new HawkFileArchiveItem { name = HawkFile.Util_FixArchiveFilename(afd.FilePath), size = (long)afd.Size, archiveIndex = i, index = ret.Count };
				ret.Add(ai);
			}

			return ret;
		}

		public void ExtractFile(int index, Stream stream)
		{
			var data = extractor.GetEntry(index).OpenEntryStream();
			byte[] buffer = new byte[1024];
			int amt = 0;
			while((amt = data.Read(buffer, 0, buffer.Length)) > 0){
				stream.Write(buffer, 0, amt);
			}
			data.Close();
		}
	}

}