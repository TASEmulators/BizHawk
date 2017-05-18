using System;
using System.IO;

using ICSharpCode.SharpZipLib.Zip;

namespace BizHawk.Client.Common
{
	public class SharpZipWriter : IZipWriter
	{
		private readonly int _level;
		private ZipOutputStream _zipOutputStream;

		public SharpZipWriter(string path, int compressionlevel)
		{
			_level = compressionlevel;
			_zipOutputStream = new ZipOutputStream(new FileStream(path, FileMode.Create, FileAccess.Write))
			{
				IsStreamOwner = true,
				UseZip64 = UseZip64.Off
			};
			_zipOutputStream.SetLevel(_level);
		}

		public void WriteItem(string name, Action<Stream> callback)
		{
			var e = new ZipEntry(name)
			{
				CompressionMethod = _level == 0 ? CompressionMethod.Stored : CompressionMethod.Deflated
			};

			_zipOutputStream.PutNextEntry(e);
			callback(_zipOutputStream);
			_zipOutputStream.CloseEntry();
		}

		public void Dispose()
		{
			if (_zipOutputStream != null)
			{
				_zipOutputStream.Dispose();
				_zipOutputStream = null;
			}
		}
	}
}
