using System;
using System.IO;

using Ionic.Zip;

namespace BizHawk.Client.Common
{
	public class IonicZipWriter : IZipWriter
	{
		private readonly int _level;
		private ZipOutputStream _zipOutputStream;

		public IonicZipWriter(string path, int compressionlevel)
		{
			_level = compressionlevel;
			_zipOutputStream = new ZipOutputStream(path)
			{
				EnableZip64 = Zip64Option.Never,
				CompressionLevel = (Ionic.Zlib.CompressionLevel)_level,
				CompressionMethod = CompressionMethod.Deflate
			};
		}

		public void WriteItem(string name, Action<Stream> callback)
		{
			var e = _zipOutputStream.PutNextEntry(name);
			e.CompressionMethod = _level == 0
				? CompressionMethod.None
				: CompressionMethod.Deflate;

			callback(_zipOutputStream); // there is no CloseEntry() call
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
