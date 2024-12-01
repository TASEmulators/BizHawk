using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

using BizHawk.Common;

namespace BizHawk.Client.Common
{
	public class ZipStateLoader : IDisposable
	{
		private ZipArchive _zip;
		private Version _ver;
		private bool _isDisposed;
		private Dictionary<string, ZipArchiveEntry> _entriesByName;
		private readonly Zstd _zstd;

		private ZipStateLoader()
		{
			_zstd = new();
		}

		public void Dispose()
		{
			Dispose(true);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				_isDisposed = true;
				if (disposing)
				{
					_zip.Dispose();
					_zstd.Dispose();
				}
			}
		}

		private void ReadZipVersion(Stream s, long length)
		{
			// the "BizState 1.0" tag contains an integer in it describing the sub version.
			if (length == 0)
			{
				_ver = new Version(1, 0, 0); // except for the first release, which doesn't
			}
			else
			{
				var sr = new StreamReader(s);
				_ver = new Version(1, 0, int.Parse(sr.ReadLine()));
			}

			Console.WriteLine("Read a zipstate of version {0}", _ver);
		}

		private void PopulateEntries()
		{
			_entriesByName = new Dictionary<string, ZipArchiveEntry>();
			foreach (var z in _zip.Entries)
			{
				string name = z.FullName;
				int i;
				if ((i = name.LastIndexOf('.')) != -1)
				{
					name = name.Substring(0, i);
				}

				_entriesByName.Add(name.Replace('\\', '/'), z);
			}
		}

		private static readonly byte[] Zipheader = { 0x50, 0x4b, 0x03, 0x04 };
		public static ZipStateLoader LoadAndDetect(string filename, bool isMovieLoad = false)
		{
			var ret = new ZipStateLoader();

			using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
			{
				byte[] data = new byte[4];
				_ = fs.Read(data, offset: 0, count: data.Length); // if stream is too short, the next check will catch it
				if (!data.SequenceEqual(Zipheader))
				{
					return null;
				}
			}

			try
			{
				ret._zip = new ZipArchive(new FileStream(filename, FileMode.Open, FileAccess.Read), ZipArchiveMode.Read);
				ret.PopulateEntries();
				if (isMovieLoad)
				{
					if (!ret.GetLump(BinaryStateLump.ZipVersion, false, ret.ReadZipVersion, false))
					{
						// movies before 1.0.2 did not include the BizState 1.0 file, don't strictly error in this case
						ret._ver = new Version(1, 0, 0);
						Console.WriteLine("Read a zipstate of version {0}", ret._ver);
					}
				}
				else if (!ret.GetLump(BinaryStateLump.ZipVersion, false, ret.ReadZipVersion, false))
				{
					ret._zip.Dispose();
					return null;
				}

				return ret;
			}
			catch (IOException)
			{
				return null;
			}
		}

		/// <param name="lump">lump to retrieve</param>
		/// <param name="abort">pass true to throw exception instead of returning false</param>
		/// <param name="callback">function to call with the desired stream</param>
		/// <param name="isZstdCompressed">lump is zstd compressed</param>
		/// <returns>true iff stream was loaded</returns>
		/// <exception cref="Exception">stream not found and <paramref name="abort"/> is <see langword="true"/></exception>
		public bool GetLump(BinaryStateLump lump, bool abort, Action<Stream, long> callback, bool isZstdCompressed = true)
		{
			if (_entriesByName.TryGetValue(lump.ReadName, out var e))
			{
				using var zs = e.Open();

				if (isZstdCompressed && _ver.Build > 1)
				{
					using var z = _zstd.CreateZstdDecompressionStream(zs);
					callback(z, e.Length);
				}
				else
				{
					callback(zs, e.Length);
				}

				return true;
			}

			if (abort)
			{
				throw new Exception($"Essential zip section not found: {lump.ReadName}");
			}

			return false;
		}

		public bool GetLump(BinaryStateLump lump, bool abort, Action<BinaryReader> callback)
			=> GetLump(lump, abort, (s, _) => callback(new(s)));

		public bool GetLump(BinaryStateLump lump, bool abort, Action<TextReader> callback)
			=> GetLump(lump, abort, (s, _) => callback(new StreamReader(s)), false);

		/// <exception cref="Exception">couldn't find Binary or Text savestate</exception>
		public void GetCoreState(Action<BinaryReader> callbackBinary, Action<TextReader> callbackText)
		{
			if (!GetLump(BinaryStateLump.Corestate, false, callbackBinary)
				&& !GetLump(BinaryStateLump.CorestateText, false, callbackText))
			{
				throw new Exception("Couldn't find Binary or Text savestate");
			}
		}
	}
}
