using System;
using System.IO;

namespace BizHawk.Client.Common
{
	public class BinaryStateSaver : IDisposable
	{
		private readonly IZipWriter _zip;
		private bool _isDisposed;

		private static void WriteVersion(Stream s)
		{
			var sw = new StreamWriter(s);
			sw.WriteLine("1"); // version 1.0.1
			sw.Flush();
		}

		private static void WriteEmuVersion(Stream s)
		{
			var sw = new StreamWriter(s);
			sw.WriteLine(VersionInfo.GetEmuVersion());
			sw.Flush();
		}

		public BinaryStateSaver(string path, int compressionLevel)
		{
			_zip = new FrameworkZipWriter(path, compressionLevel);
		}

		public void PutVersionLumps()
		{
			PutLump(BinaryStateLump.Versiontag, WriteVersion);
			PutLump(BinaryStateLump.BizVersion, WriteEmuVersion);
		}

		public void PutLump(BinaryStateLump lump, Action<Stream> callback)
		{
			_zip.WriteItem(lump.WriteName, callback);
		}

		public void PutLump(BinaryStateLump lump, Action<BinaryWriter> callback)
		{
			PutLump(lump, delegate(Stream s)
			{
				var bw = new BinaryWriter(s);
				callback(bw);
				bw.Flush();
			});
		}

		public void PutLump(BinaryStateLump lump, Action<TextWriter> callback)
		{
			PutLump(lump, delegate(Stream s)
			{
				TextWriter tw = new StreamWriter(s);
				callback(tw);
				tw.Flush();
			});
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
