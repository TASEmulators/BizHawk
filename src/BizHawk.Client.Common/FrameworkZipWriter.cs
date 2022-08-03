using System;
using System.IO;
using System.IO.Compression;

namespace BizHawk.Client.Common
{
	public class FrameworkZipWriter : IZipWriter
	{
		private ZipArchive _archive;
		private readonly CompressionLevel _level;
		private readonly int _zstdCompressionLevel;

		public FrameworkZipWriter(string path, int compressionLevel)
		{
			_archive = new ZipArchive(new FileStream(path, FileMode.Create, FileAccess.Write),
				ZipArchiveMode.Create, false);
			if (compressionLevel == 0)
				_level = CompressionLevel.NoCompression;
			else if (compressionLevel < 5)
				_level = CompressionLevel.Fastest;
			else
				_level = CompressionLevel.Optimal;

			// compressionLevel ranges from 0 to 9
			// normal compression level range for zstd is 1 to 19
			_zstdCompressionLevel = compressionLevel * 2 + 1;
		}

		public void WriteItem(string name, Action<Stream> callback, bool doubleCompress)
		{
			using var stream = _archive.CreateEntry(name, _level).Open();

			if (doubleCompress)
			{
				using var z = Zstd.Zstd.CreateZstdCompressionStream(stream, _zstdCompressionLevel);
				callback(z);
			}
			else
			{
				callback(stream);
			}
		}

		public void Dispose()
		{
			if (_archive != null)
			{
				_archive.Dispose();
				_archive = null;
			}
		}
	}
}
