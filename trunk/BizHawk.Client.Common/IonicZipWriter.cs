using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Ionic.Zip;
using System.IO;

namespace BizHawk.Client.Common
{
	public class IonicZipWriter : IZipWriter
	{
		private ZipOutputStream z;
		private int level;

		public IonicZipWriter(Stream s, int compressionlevel)
		{
			level = compressionlevel;
			z = new ZipOutputStream(s, true)
			{
				EnableZip64 = Zip64Option.Never,
				CompressionLevel = (Ionic.Zlib.CompressionLevel)level
			};
		}

		public void WriteItem(string name, Action<Stream> callback)
		{
			var e = z.PutNextEntry(name);
			if (level == 0)
				e.CompressionMethod = CompressionMethod.None;
			else
				e.CompressionMethod = CompressionMethod.Deflate;
			callback(z);
			// there is no CloseEntry() call
		}

		public void Dispose()
		{
			if (z != null)
			{
				z.Dispose();
				z = null;
			}
		}
	}
}
