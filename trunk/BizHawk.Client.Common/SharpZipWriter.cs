using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ICSharpCode.SharpZipLib.Zip;
using System.IO;

namespace BizHawk.Client.Common
{
	public class SharpZipWriter : IZipWriter
	{
		private ZipOutputStream z;
		private int level;

		public SharpZipWriter(string path, int compressionlevel)
		{
			level = compressionlevel;
			z = new ZipOutputStream(new FileStream(path, FileMode.Create, FileAccess.Write))
			{
				IsStreamOwner = true,
				UseZip64 = UseZip64.Off
			};
			z.SetLevel(level);
		}

		public void WriteItem(string name, Action<Stream> callback)
		{
			var e = new ZipEntry(name);
			if (level == 0)
				e.CompressionMethod = CompressionMethod.Stored;
			else
				e.CompressionMethod = CompressionMethod.Deflated;
			z.PutNextEntry(e);
			callback(z);
			z.CloseEntry();		
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
