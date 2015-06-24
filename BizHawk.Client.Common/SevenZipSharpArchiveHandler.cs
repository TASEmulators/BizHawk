using System.Collections.Generic;
using System.IO;
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
        private IArchive _extractor;
        private FileStream _stream;

		public void Dispose()
		{
			if (_extractor != null)
			{
				_extractor.Dispose();
				_extractor = null;
			}
			if (_stream != null) 
			{
				_stream.Dispose ();
				_stream = null;
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
				catch
				{
					return false;
				}
			}

			return true; //This is an archive
		}

		public IHawkFileArchiveHandler Construct(string path)
		{
			var ret = new SevenZipSharpArchiveHandler();
			ret.Open(path);
			return ret;
		}

		private void Open(string path)
		{
			if (_stream != null) {
				_stream.Dispose();
				_stream = null;
			}
			_stream = new FileStream(path, FileMode.Open);
			_extractor = ArchiveFactory.Open(_stream, SharpCompress.Common.Options.None);
		}
        		
		public List<HawkFileArchiveItem> Scan()
		{
			List<HawkFileArchiveItem> ret = new List<HawkFileArchiveItem>();
			int i = -1;
			foreach(var afd in _extractor.Entries)
			{
				i++;
				if (afd.IsDirectory) continue;
				var ai = new HawkFileArchiveItem { Name = HawkFile.Util_FixArchiveFilename(afd.FilePath), Size = (long)afd.Size, ArchiveIndex = i, Index = ret.Count };
				ret.Add(ai);
			}

			return ret;
		}

		public void ExtractFile(int index, Stream stream)
		{
			var data = _extractor.GetEntry(index).OpenEntryStream();
			byte[] buffer = new byte[1024];
			int amt = 0;
			while((amt = data.Read(buffer, 0, buffer.Length)) > 0){
				stream.Write(buffer, 0, amt);
			}
			data.Close();
		}
	}

}