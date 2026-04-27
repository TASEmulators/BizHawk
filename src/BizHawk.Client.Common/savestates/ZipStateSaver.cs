#nullable enable

using System.IO;

using BizHawk.Common;

namespace BizHawk.Client.Common
{
	public class ZipStateSaver : IDisposable
	{
		internal const string TOP_LEVEL_DIR_NAME = "BizState"; // savestates and movies all have the same structure, including the `BizState 1.0` file, so this seemed a fitting name

		private readonly IZipWriter _zip;
		private bool _isDisposed;

		public bool AsTarbomb = /*!OSTailoredCode.IsUnixHost*/true;

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

		private ZipStateSaver(FrameworkZipWriter zip)
		{
			_zip = zip;

			// we put these in every zip, so we know where they came from
			// a bit redundant for movie files given their headers, but w/e
			PutLump(BinaryStateLump.ZipVersion, WriteZipVersion, false);
			PutLump(BinaryStateLump.BizVersion, WriteEmuVersion, false);
		}

		public static FileWriteResult<ZipStateSaver> Create(string path, int compressionLevel)
		{
			FileWriteResult<FrameworkZipWriter> result = FrameworkZipWriter.Create(path, compressionLevel);
			if (result.IsError) return new(result);
			else return result.Convert(new ZipStateSaver(result.Value!));
		}

		/// <summary>
		/// This method must be called after writing has finished and must not be called twice.
		/// Dispose will be called regardless of the result.
		/// </summary>
		public FileWriteResult CloseAndDispose()
		{
			FileWriteResult result = _zip.CloseAndDispose();
			Dispose();
			return result;
		}

		/// <summary>
		/// Closes and deletes the file. Use if there was an error while writing.
		/// Do not call <see cref="CloseAndDispose"/> after this.
		/// </summary>
		public void Abort()
		{
			_zip.Abort();
			Dispose();
		}

		public void PutLump(BinaryStateLump lump, Action<Stream> callback, bool zstdCompress = true)
		{
			var filePath = AsTarbomb ? lump.FileName : $"{TOP_LEVEL_DIR_NAME}/{lump.FileName}";
			if (zstdCompress)
			{
				_zip.WriteItem(filePath + ".zst", callback, zstdCompress: true);
			}
			else
			{
				_zip.WriteItem(filePath, callback, zstdCompress: false);
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

		public void PutLump(BinaryStateLump lump, Action<TextWriter> callback, bool zstdCompress = false)
		{
			PutLump(lump, s =>
			{
				TextWriter tw = new StreamWriter(s);
				callback(tw);
				tw.Flush();
			}, zstdCompress: zstdCompress);
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
