using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace BizHawk.Client.Common
{
	public class FrameworkZipWriter : IZipWriter
	{
		private ZipArchive _archive;
		private readonly CompressionLevel _level;

		public FrameworkZipWriter(string path, int compressionlevel)
		{
			_archive = new ZipArchive(new FileStream(path, FileMode.Create, FileAccess.Write),
				ZipArchiveMode.Create, false);
			if (compressionlevel == 0)
				_level = CompressionLevel.NoCompression;
			else if (compressionlevel < 5)
				_level = CompressionLevel.Fastest;
			else
				_level = CompressionLevel.Optimal;
		}

		public void WriteItem(string name, Action<Stream> callback)
		{
			using (var stream = _archive.CreateEntry(name, _level).Open())
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
