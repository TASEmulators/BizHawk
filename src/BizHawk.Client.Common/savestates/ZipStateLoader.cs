using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace BizHawk.Client.Common
{
	public class ZipStateLoader : IDisposable
	{
		private ZipArchive _zip;
		private Version _ver;
		private bool _isDisposed;
		private Dictionary<string, ZipArchiveEntry> _entriesByName;

		private ZipStateLoader()
		{
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
				}
			}
		}

		private void ReadVersion(Stream s, long length)
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
				fs.Read(data, 0, 4);
				if (!data.SequenceEqual(Zipheader))
				{
					return null;
				}
			}

			try
			{
				ret._zip = new ZipArchive(new FileStream(filename, FileMode.Open, FileAccess.Read), ZipArchiveMode.Read);
				ret.PopulateEntries();
				if (!isMovieLoad && !ret.GetLump(BinaryStateLump.Versiontag, false, ret.ReadVersion))
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
		/// <returns>true iff stream was loaded</returns>
		/// <exception cref="Exception">stream not found and <paramref name="abort"/> is <see langword="true"/></exception>
		public bool GetLump(BinaryStateLump lump, bool abort, Action<Stream, long> callback)
		{
			if (_entriesByName.TryGetValue(lump.ReadName, out var e))
			{
				using var zs = e.Open();
				callback(zs, e.Length);

				return true;
			}

			if (abort)
			{
				throw new Exception($"Essential zip section not found: {lump.ReadName}");
			}

			return false;
		}

		public bool GetLump(BinaryStateLump lump, bool abort, Action<BinaryReader> callback)
		{
			return GetLump(lump, abort, delegate(Stream s, long unused)
			{
				var br = new BinaryReader(s);
				callback(br);
			});
		}

		public bool GetLump(BinaryStateLump lump, bool abort, Action<BinaryReader, long> callback)
		{
			return GetLump(lump, abort, delegate(Stream s, long length)
			{
				var br = new BinaryReader(s);
				callback(br, length);
			});
		}

		public bool GetLump(BinaryStateLump lump, bool abort, Action<TextReader> callback)
		{
			return GetLump(lump, abort, delegate(Stream s, long unused)
			{
				var tr = new StreamReader(s);
				callback(tr);
			});
		}

		/// <exception cref="Exception">couldn't find Binary or Text savestate</exception>
		public void GetCoreState(Action<BinaryReader, long> callbackBinary, Action<TextReader> callbackText)
		{
			if (!GetLump(BinaryStateLump.Corestate, false, callbackBinary)
				&& !GetLump(BinaryStateLump.CorestateText, false, callbackText))
			{
				throw new Exception("Couldn't find Binary or Text savestate");
			}
		}

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
