#nullable enable

using System.IO;
using System.IO.Compression;

using BizHawk.Common;

namespace BizHawk.Client.Common
{
	public class FrameworkZipWriter : IZipWriter
	{
		private ZipArchive? _archive;

		private FileWriter? _fs;

		private Zstd? _zstd;
		private readonly CompressionLevel _level;
		private readonly int _zstdCompressionLevel;

		private Exception? _writeException = null;
		private bool _disposed;

		private FrameworkZipWriter(int compressionLevel)
		{
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

		public static FileWriteResult<FrameworkZipWriter> Create(string path, int compressionLevel)
		{
			FileWriteResult<FileWriter> fs = FileWriter.Create(path);
			if (fs.IsError) return new(fs);

			FrameworkZipWriter ret = new(compressionLevel);
			ret._fs = fs.Value!;
			ret._archive = new(ret._fs.Stream, ZipArchiveMode.Create, leaveOpen: true);

			return fs.Convert(ret);
		}

		public FileWriteResult CloseAndDispose()
		{
			if (_archive == null || _fs == null) throw new ObjectDisposedException("Cannot use disposed ZipWriter.");

			// We actually have to do this here since it has to be done before the file stream is closed.
			_archive.Dispose();
			_archive = null;

			FileWriteResult ret;
			if (_writeException == null)
			{
				ret = _fs.CloseAndDispose();
			}
			else
			{
				ret = new(FileWriteEnum.FailedDuringWrite, _fs.TempPath, _writeException);
				_fs.Abort();
			}

			// And since we have to close stuff, there's really no point in not disposing here.
			Dispose();
			return ret;
		}

		public void Abort()
		{
			if (_archive == null || _fs == null) throw new ObjectDisposedException("Cannot use disposed ZipWriter.");

			_archive.Dispose();
			_archive = null;

			_fs.Abort();

			Dispose();
		}

		public void WriteItem(string name, Action<Stream> callback, bool zstdCompress)
		{
			if (_archive == null || _zstd == null) throw new ObjectDisposedException("Cannot use disposed ZipWriter.");
			if (_writeException != null) return;

			try
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
			catch (Exception ex)
			{
				_writeException = ex;
				// We aren't returning the failure until closing. Should we? I don't want to refactor that much calling code without a good reason.
			}
		}

		public void Dispose()
		{
			if (_disposed) return;
			_disposed = true;

			// _archive should already be disposed by CloseAndDispose, but just in case
			_archive?.Dispose();
			_archive = null;
			_zstd!.Dispose();
			_zstd = null;

			_fs!.Dispose();
			_fs = null;
		}
	}
}
