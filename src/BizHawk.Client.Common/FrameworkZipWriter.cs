using System.IO;
using System.IO.Compression;

using BizHawk.Common;

namespace BizHawk.Client.Common
{
	public class FrameworkZipWriter : IZipWriter
	{
		private ZipArchive _archive;
		private Zstd _zstd;
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

			_zstd = new();
			// compressionLevel ranges from 0 to 9
			// normal compression level range for zstd is 1 to 19
			_zstdCompressionLevel = compressionLevel * 2 + 1;
		}

		public void WriteItem(string name, Action<Stream> callback, bool zstdCompress)
		{
			// don't compress with deflate if we're already compressing with zstd
			// this won't produce meaningful compression, and would just be a timesink
			using var stream = _archive.CreateEntry(name, zstdCompress ? CompressionLevel.NoCompression : _level).Open();

			if (zstdCompress)
			{
				using var z = _zstd.CreateZstdCompressionStream(stream, _zstdCompressionLevel);
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

			if (_zstd != null)
			{
				_zstd.Dispose();
				_zstd = null;
			}
		}
	}
}
