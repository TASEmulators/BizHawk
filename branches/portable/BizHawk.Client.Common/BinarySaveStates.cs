using System;
using System.Collections.Generic;
using System.IO;

using ICSharpCode.SharpZipLib.Zip;
//using Ionic.Zip;

namespace BizHawk.Client.Common
{
	public enum BinaryStateLump
	{
		Versiontag,
		Corestate,
		Framebuffer,
		Input,
		CorestateText,

		// Only for movies they probably shoudln't be leaching this stuff
		Movieheader,
		Comments,
		Subtitles,
		SyncSettings,

		// TasMovie
		LagLog,
		Greenzone,
		GreenzoneSettings,
		Markers,
		ClientSettings
	}

	public static class BinaryStateFileNames
	{
		private static readonly Dictionary<BinaryStateLump, string> ReadNames;
		private static readonly Dictionary<BinaryStateLump, string> WriteNames;

		static void AddLumpName(BinaryStateLump token, string name)
		{
			ReadNames[token] = Path.GetFileNameWithoutExtension(name);
			WriteNames[token] = name;
		}
		static BinaryStateFileNames()
		{
			ReadNames = new Dictionary<BinaryStateLump, string>();
			WriteNames = new Dictionary<BinaryStateLump, string>();
			AddLumpName(BinaryStateLump.Versiontag, "BizState 1.0");
			AddLumpName(BinaryStateLump.Corestate, "Core");
			AddLumpName(BinaryStateLump.Framebuffer, "Framebuffer");
			AddLumpName(BinaryStateLump.Input, "Input Log.txt");
			AddLumpName(BinaryStateLump.CorestateText, "CoreText.txt");
			AddLumpName(BinaryStateLump.Movieheader, "Header.txt");

			// Only for movies they probably shoudln't be leaching this stuff
			AddLumpName(BinaryStateLump.Comments, "Comments.txt");
			AddLumpName(BinaryStateLump.Subtitles, "Subtitles.txt");
			AddLumpName(BinaryStateLump.SyncSettings, "SyncSettings.json");

			// TasMovie
			AddLumpName(BinaryStateLump.LagLog, "LagLog");
			AddLumpName(BinaryStateLump.Greenzone, "GreenZone");
			AddLumpName(BinaryStateLump.GreenzoneSettings, "GreenZoneSettings.txt");
			AddLumpName(BinaryStateLump.Markers, "Markers.txt");
			AddLumpName(BinaryStateLump.ClientSettings, "ClientSettings.json");
		}

		public static string GetReadName(BinaryStateLump lump)
		{
			return ReadNames[lump];
		}
		public static string GetWriteName(BinaryStateLump lump)
		{
			return WriteNames[lump];
		}
	}

	/// <summary>
	/// more accurately should be called ZipStateLoader, as it supports both text and binary core data
	/// </summary>
	public class BinaryStateLoader : IDisposable
	{
		private ZipFile _zip;
		private Version _ver;
		private bool _isDisposed;
		private Dictionary<string, ZipEntry> _entriesbyname;

		private BinaryStateLoader()
		{
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
					_zip.Close();
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
			_entriesbyname = new Dictionary<string, ZipEntry>();
			foreach (ZipEntry z in _zip)
			{
				_entriesbyname.Add(Path.GetFileNameWithoutExtension(z.Name), z);
			}
		}

		public static BinaryStateLoader LoadAndDetect(string filename, bool isMovieLoad = false)
		{
			var ret = new BinaryStateLoader();

			// PORTABLE TODO - SKIP THIS.. FOR NOW
			// check whether its an archive before we try opening it
			bool isArchive;
			using (var archiveChecker = new SevenZipSharpArchiveHandler())
			{
				int offset;
				bool isExecutable;
				isArchive = archiveChecker.CheckSignature(filename, out offset, out isExecutable);
			}

			if (!isArchive)
			{
				return null;
			}

			try
			{
				ret._zip = new ZipFile(filename);
				ret.PopulateEntries();
				if (!isMovieLoad && !ret.GetLump(BinaryStateLump.Versiontag, false, ret.ReadVersion))
				{
					ret._zip.Close();
					return null;
				}

				return ret;
			}
			catch (ZipException)
			{
				return null;
			}
		}

		public bool HasLump(BinaryStateLump lump)
		{
			string name = BinaryStateFileNames.GetReadName(lump);
			ZipEntry e;
			return _entriesbyname.TryGetValue(name, out e);
		}

		/// <summary>
		/// Gets a lump
		/// </summary>
		/// <param name="lump">lump to retriever</param>
		/// <param name="abort">true to throw exception on failure</param>
		/// <param name="callback">function to call with the desired stream</param>
		/// <returns>true if callback was called and stream was loaded</returns>
		public bool GetLump(BinaryStateLump lump, bool abort, Action<Stream, long> callback)
		{
			string name = BinaryStateFileNames.GetReadName(lump);
			ZipEntry e;
			if (_entriesbyname.TryGetValue(name, out e))
			{
				using (var zs = _zip.GetInputStream(e))
				{
					callback(zs, e.Size);
				}

				return true;
			}
			
			if (abort)
			{
				throw new Exception("Essential zip section not found: " + name);
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

		public void GetCoreState(Action<BinaryReader, long> callbackBinary, Action<TextReader> callbackText)
		{
			if (!GetLump(BinaryStateLump.Corestate, false, callbackBinary)
			    && !GetLump(BinaryStateLump.CorestateText, false, callbackText))
			{
				throw new Exception("Couldn't find Binary or Text savestate");
			}
		}

		public void GetCoreState(Action<BinaryReader> callbackBinary, Action<TextReader> callbackText)
		{
			if (!GetLump(BinaryStateLump.Corestate, false, callbackBinary)
				&& !GetLump(BinaryStateLump.CorestateText, false, callbackText))
			{
				throw new Exception("Couldn't find Binary or Text savestate");
			}
		}
	}

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


		public BinaryStateSaver(string path, bool stateVersionTag = true) // stateVersionTag is a hack for reusing this for movie code
		{
			//_zip = new IonicZipWriter(path, Global.Config.SaveStateCompressionLevelNormal);
			_zip = new SharpZipWriter(path, Global.Config.SaveStateCompressionLevelNormal);
			//_zip = new SevenZipWriter(path, Global.Config.SaveStateCompressionLevelNormal);

			if (stateVersionTag)
			{
				PutLump(BinaryStateLump.Versiontag, WriteVersion);
			}
		}

		public void PutLump(BinaryStateLump lump, Action<Stream> callback)
		{
			var name = BinaryStateFileNames.GetWriteName(lump);
			_zip.WriteItem(name, callback);
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
