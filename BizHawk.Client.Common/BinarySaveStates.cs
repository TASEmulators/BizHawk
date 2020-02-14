using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ICSharpCode.SharpZipLib.Zip;

namespace BizHawk.Client.Common
{
	public class BinaryStateLump
	{
		[Name("BizState 1", "0")]
		public static BinaryStateLump Versiontag { get; private set; }
		[Name("BizVersion", "txt")]
		public static BinaryStateLump BizVersion { get; private set; }
		[Name("Core", "bin")]
		public static BinaryStateLump Corestate { get; private set; }
		[Name("Framebuffer", "bmp")]
		public static BinaryStateLump Framebuffer { get; private set; }
		[Name("Input Log", "txt")]
		public static BinaryStateLump Input { get; private set; }
		[Name("CoreText", "txt")]
		public static BinaryStateLump CorestateText { get; private set; }
		[Name("MovieSaveRam", "bin")]
		public static BinaryStateLump MovieSaveRam { get; private set; }

		// Only for movies they probably shouldn't be leaching this stuff
		[Name("Header", "txt")]
		public static BinaryStateLump Movieheader { get; private set; }
		[Name("Comments", "txt")]
		public static BinaryStateLump Comments { get; private set; }
		[Name("Subtitles", "txt")]
		public static BinaryStateLump Subtitles { get; private set; }
		[Name("SyncSettings", "json")]
		public static BinaryStateLump SyncSettings { get; private set; }

		// TasMovie
		[Name("LagLog")]
		public static BinaryStateLump LagLog { get; private set; }
		[Name("GreenZone")]
		public static BinaryStateLump StateHistory { get; private set; }
		[Name("GreenZoneSettings", "txt")]
		public static BinaryStateLump StateHistorySettings { get; private set; }
		[Name("Markers", "txt")]
		public static BinaryStateLump Markers { get; private set; }
		[Name("ClientSettings", "json")]
		public static BinaryStateLump ClientSettings { get; private set; }
		[Name("VerificationLog", "txt")]
		public static BinaryStateLump VerificationLog { get; private set; }
		[Name("UserData", "txt")]
		public static BinaryStateLump UserData { get; private set; }
		[Name("Session", "txt")]
		public static BinaryStateLump Session { get; private set; }

		// branch stuff
		[Name("Branches\\CoreData", "bin")]
		public static BinaryStateLump BranchCoreData { get; private set; }
		[Name("Branches\\InputLog", "txt")]
		public static BinaryStateLump BranchInputLog { get; private set; }
		[Name("Branches\\FrameBuffer", "bmp")]
		public static BinaryStateLump BranchFrameBuffer { get; private set; }
		[Name("Branches\\CoreFrameBuffer", "bmp")]
		public static BinaryStateLump BranchCoreFrameBuffer { get; private set; }
		[Name("Branches\\Header", "json")]
		public static BinaryStateLump BranchHeader { get; private set; }
		[Name("Branches\\Markers", "txt")]
		public static BinaryStateLump BranchMarkers { get; private set; }
		[Name("Branches\\UserText", "txt")]
		public static BinaryStateLump BranchUserText { get; private set; }

		[AttributeUsage(AttributeTargets.Property)]
		private class NameAttribute : Attribute
		{
			public string Name { get; }
			public string Ext { get; }
			public NameAttribute(string name)
			{
				Name = name;
			}

			public NameAttribute(string name, string ext)
			{
				Name = name;
				Ext = ext;
			}
		}

		public string ReadName => Name;
		public string WriteName => Ext != null ? Name + '.' + Ext : Name;

		public string Name { get; protected set; }
		public string Ext { get; protected set; }

		private BinaryStateLump(string name, string ext)
		{
			Name = name;
			Ext = ext;
		}

		protected BinaryStateLump()
		{
		}

		static BinaryStateLump()
		{
			foreach (var prop in typeof(BinaryStateLump).GetProperties(BindingFlags.Public | BindingFlags.Static))
			{
				var attr = prop.GetCustomAttributes(false).OfType<NameAttribute>().Single();
				object value = new BinaryStateLump(attr.Name, attr.Ext);
				prop.SetValue(null, value, null);
			}
		}
	}

	/// <summary>
	/// describes a BinaryStateLump virtual name that has a numerical index
	/// </summary>
	public class IndexedStateLump : BinaryStateLump
	{
		private readonly BinaryStateLump _root;
		private int _idx;
		public IndexedStateLump(BinaryStateLump root)
		{
			_root = root;
			Ext = _root.Ext;
			Calc();
		}

		private void Calc()
		{
			Name = _root.Name + _idx;
		}

		public void Increment()
		{
			_idx++;
			Calc();
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
		private Dictionary<string, ZipEntry> _entriesByName;

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
			_entriesByName = new Dictionary<string, ZipEntry>();
			foreach (ZipEntry z in _zip)
			{
				string name = z.Name;
				int i;
				if ((i = name.LastIndexOf('.')) != -1)
				{
					name = name.Substring(0, i);
				}

				_entriesByName.Add(name.Replace('/', '\\'), z);
			}
		}

		private static readonly byte[] Zipheader = { 0x50, 0x4b, 0x03, 0x04 };
		public static BinaryStateLoader LoadAndDetect(string filename, bool isMovieLoad = false)
		{
			var ret = new BinaryStateLoader();

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

		/// <param name="lump">lump to retrieve</param>
		/// <param name="abort">pass true to throw exception instead of returning false</param>
		/// <param name="callback">function to call with the desired stream</param>
		/// <returns>true iff stream was loaded</returns>
		/// <exception cref="Exception">stream not found and <paramref name="abort"/> is <see langword="true"/></exception>
		public bool GetLump(BinaryStateLump lump, bool abort, Action<Stream, long> callback)
		{
			ZipEntry e;
			if (_entriesByName.TryGetValue(lump.ReadName, out e))
			{
				using (var zs = _zip.GetInputStream(e))
				{
					callback(zs, e.Size);
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
		public void GetCoreState(Action<BinaryReader, long> callbackBinary)
		{
			if (!GetLump(BinaryStateLump.Corestate, false, callbackBinary))
			{
				throw new Exception("Could not find Core Savestate");
			}
		}

		/// <exception cref="Exception">couldn't find Binary or Text savestate</exception>
		public void GetCoreState(Action<BinaryReader> callbackBinary)
		{
			if (!GetLump(BinaryStateLump.Corestate, false, callbackBinary))
			{
				throw new Exception("Could not find Core Savestate");
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

		private static void WriteEmuVersion(Stream s)
		{
			var sw = new StreamWriter(s);
			sw.WriteLine(VersionInfo.GetEmuVersion());
			sw.Flush();
		}

		public BinaryStateSaver(string path, bool notamovie = true) // notamovie is hack, really should have separate something
		{
			_zip = new FrameworkZipWriter(path, notamovie ? Global.Config.SaveStateCompressionLevelNormal : Global.Config.MovieCompressionLevel);

			if (notamovie)
			{
				PutLump(BinaryStateLump.Versiontag, WriteVersion);
				PutLump(BinaryStateLump.BizVersion, WriteEmuVersion);
			}
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
