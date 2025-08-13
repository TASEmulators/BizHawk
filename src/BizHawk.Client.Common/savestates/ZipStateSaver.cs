using System.IO;

using BizHawk.Common;

namespace BizHawk.Client.Common
{
	public class ZipStateSaver : IDisposable
	{
		private readonly IZipWriter _zip;
		private bool _isDisposed;

		private static void WriteZipVersion(Stream s)
		{
			using var sw = new StreamWriter(s);
			sw.WriteLine("3"); // version 1.0.3
		}

		private static void WriteEmuVersion(Stream s)
		{
			using var sw = new StreamWriter(s);
			sw.WriteLine(VersionInfo.GetEmuVersion());
		}

		public ZipStateSaver(string path, int compressionLevel)
		{
			_zip = new FrameworkZipWriter(path, compressionLevel);

			// we put these in every zip, so we know where they came from
			// a bit redundant for movie files given their headers, but w/e
			PutLump(BinaryStateLump.ZipVersion, WriteZipVersion, false);
			PutLump(BinaryStateLump.BizVersion, WriteEmuVersion, false);
		}

		public void PutLump(BinaryStateLump lump, Action<Stream> callback, bool zstdCompress = true)
		{
			if (zstdCompress)
			{
				_zip.WriteItem(lump.FileName + ".zst", callback, true);
			}
			else
			{
				_zip.WriteItem(lump.FileName, callback, false);
			}
		}

		public void PutLump(BinaryStateLump lump, Action<BinaryWriter> callback)
		{
			PutLump(lump, s =>
			{
				var bw = new BinaryWriter(s);
				callback(bw);
				bw.Flush();
			});
		}

		public void PutLump(BinaryStateLump lump, Action<TextWriter> callback)
		{
			// For small text files, skip compression to avoid overhead that makes them larger
			const int SMALL_TEXT_THRESHOLD = 256; // bytes
			
			// First, capture the text content to determine its size
			string textContent;
			using (var tempStream = new StringWriter())
			{
				callback(tempStream);
				textContent = tempStream.ToString();
			}
			
			var textBytes = System.Text.Encoding.UTF8.GetByteCount(textContent);
			bool shouldCompress = textBytes > SMALL_TEXT_THRESHOLD;
			
			PutLump(lump, s =>
			{
				TextWriter tw = new StreamWriter(s);
				tw.Write(textContent);
				tw.Flush();
			}, shouldCompress);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				_isDisposed = true;

				if (disposing)
				{
					_zip.Dispose();
				}
			}
		}
	}
}
