using System;
using SharpCompress.Archive;
using SharpCompress.Writer;
using BizHawk.Client.Common;
using System.IO;
using System.Collections.Generic;

namespace BizHawk.Client.Common
{

	public class BinaryStateFileNames
	{
		/*
		public const string Versiontag = "BizState 1.0";
		public const string Corestate = "Core";
		public const string Framebuffer = "Framebuffer";
		public const string Input = "Input Log";
		public const string CorestateText = "CoreText";
		public const string Movieheader = "Header";
		*/

		private static Dictionary<BinaryStateLump, string> LumpNames;

		static BinaryStateFileNames()
		{
			LumpNames = new Dictionary<BinaryStateLump, string>();
			LumpNames[BinaryStateLump.Versiontag] = "BizState 1.0";
			LumpNames[BinaryStateLump.Corestate] = "Core";
			LumpNames[BinaryStateLump.Framebuffer] = "Framebuffer";
			LumpNames[BinaryStateLump.Input] = "Input Log";
			LumpNames[BinaryStateLump.CorestateText] = "CoreText";
			LumpNames[BinaryStateLump.Movieheader] = "Header";
		}

		public static string Get(BinaryStateLump Lump) { return LumpNames[Lump]; }
	}


	public enum BinaryStateLump
	{
		Versiontag,
		Corestate,
		Framebuffer,
		Input,
		CorestateText,
		Movieheader
	};

	/// <summary>
	/// more accurately should be called ZipStateLoader, as it supports both text and binary core data
	/// </summary>
	public class BinaryStateLoader : IDisposable
	{

		private bool isDisposed;
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!isDisposed)
			{
				isDisposed = true;

				if (disposing)
				{
					zip.Dispose();
				}
			}
		}

		IArchive zip;
		Version ver;

		private BinaryStateLoader()
		{
		}

		private void ReadVersion(Stream s)
		{
			// the "BizState 1.0" tag contains an integer in it describing the sub version.
			if (s.Length == 0)
				ver = new Version(1, 0, 0); // except for the first release, which doesn't
			else
			{
				StreamReader sr = new StreamReader(s);
				ver = new Version(1, 0, int.Parse(sr.ReadLine()));
			}
			Console.WriteLine("Read a zipstate of version {0}", ver.ToString());
		}

		public static BinaryStateLoader LoadAndDetect(string Filename)
		{
			BinaryStateLoader ret = new BinaryStateLoader();

			//PORTABLE TODO - SKIP THIS.. FOR NOW
			//check whether its an archive before we try opening it
			int offset;
			bool isExecutable;
			bool isArchive;
			using(var archiveChecker = new SevenZipSharpArchiveHandler())
				isArchive = archiveChecker.CheckSignature(Filename, out offset, out isExecutable);
			if(!isArchive)
				return null;

			try
			{
				ret.zip = SharpCompress.Archive.ArchiveFactory.Open(new FileStream(Filename, FileMode.Open), SharpCompress.Common.Options.None);
				if (!ret.GetLump(BinaryStateLump.Versiontag, false, ret.ReadVersion))
				{
					ret.zip.Dispose();
					return null;
				}
				return ret;
			}
			catch (Exception)
			{
				return null;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Lump">lump to retriever</param>
		/// <param name="abort">true to throw exception on failure</param>
		/// <param name="callback">function to call with the desired stream</param>
		/// <returns>true if callback was called and stream was loaded</returns>
		public bool GetLump(BinaryStateLump Lump, bool abort, Action<Stream> callback)
		{
			string Name = BinaryStateFileNames.Get(Lump);
			var e = zip.GetEntry(Name);
			if (e != null)
			{
				using (Stream zs = e.OpenEntryStream())
				{
					callback(zs);
				}
				return true;
			}
			else if (abort)
			{
				throw new Exception("Essential zip section not found: " + Name);
			}
			else
			{
				return false;
			}
		}

		public bool GetLump(BinaryStateLump Lump, bool abort, Action<BinaryReader> callback)
		{
			return GetLump(Lump, abort, delegate(Stream s)
			{
				BinaryReader br = new BinaryReader(s);
				callback(br);
			});
		}

		public bool GetLump(BinaryStateLump Lump, bool abort, Action<TextReader> callback)
		{
			return GetLump(Lump, abort, delegate(Stream s)
			{
				TextReader tr = new StreamReader(s);
				callback(tr);
			});
		}

		/// <summary>
		/// load binary state, or text state if binary state lump doesn't exist
		/// </summary>
		/// <param name="callbackBinary"></param>
		/// <param name="callbackText"></param>
		public void GetCoreState(Action<Stream> callbackBinary, Action<Stream> callbackText)
		{
			if (!GetLump(BinaryStateLump.Corestate, false, callbackBinary)
				&& !GetLump(BinaryStateLump.CorestateText, false, callbackText))
				throw new Exception("Couldn't find Binary or Text savestate");
		}

		public void GetCoreState(Action<BinaryReader> callbackBinary, Action<TextReader> callbackText)
		{
			if (!GetLump(BinaryStateLump.Corestate, false, callbackBinary)
				&& !GetLump(BinaryStateLump.CorestateText, false, callbackText))
				throw new Exception("Couldn't find Binary or Text savestate");
		}

		/*
		public bool GetFrameBuffer(Action<Stream> callback)
		{
			return GetFileByName(BinaryStateFileNames.Framebuffer, false, callback);
		}

		public void GetInputLogRequired(Action<Stream> callback)
		{
			GetFileByName(BinaryStateFileNames.Input, true, callback);
		}

		public void GetMovieHeaderRequired(Action<Stream> callback)
		{
			GetFileByName(BinaryStateFileNames.Movieheader, true, callback);
		}
		*/
	}

	public class BinaryStateSaver : IDisposable
	{
		private readonly IWriter zip;

		private void WriteVersion(Stream s)
		{
			StreamWriter sw = new StreamWriter(s);
			sw.WriteLine("1"); // version 1.0.1
			sw.Flush();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="s">not closed when finished!</param>
		public BinaryStateSaver(Stream s)
		{
			zip = WriterFactory.Open (s, SharpCompress.Common.ArchiveType.Zip, SharpCompress.Common.CompressionType.Deflate);

			PutLump(BinaryStateLump.Versiontag, WriteVersion);	
		}

		public void PutLump(BinaryStateLump Lump, Action<Stream> callback)
		{
			string Name = BinaryStateFileNames.Get(Lump);
            MemoryStream ms = new MemoryStream();
            callback(ms);
            ms.Position = 0;
            zip.Write(Name, ms);
		}

		public void PutLump(BinaryStateLump Lump, Action<BinaryWriter> callback)
		{
			PutLump(Lump, delegate(Stream s)
			{
				BinaryWriter bw = new BinaryWriter(s);
				callback(bw);
				bw.Flush();
			});
		}

		public void PutLump(BinaryStateLump Lump, Action<TextWriter> callback)
		{
			PutLump(Lump, delegate(Stream s)
			{
				TextWriter tw = new StreamWriter(s);
				callback(tw);
				tw.Flush();
			});
		}

		/*
		public void PutCoreStateBinary(Action<Stream> callback)
		{
			PutFileByName(BinaryStateFileNames.Corestate, callback);
		}

		public void PutCoreStateText(Action<Stream> callback)
		{
			PutFileByName(BinaryStateFileNames.CorestateText, callback);
		}

		public void PutFrameBuffer(Action<Stream> callback)
		{
			PutFileByName(BinaryStateFileNames.Framebuffer, callback);
		}

		public void PutInputLog(Action<Stream> callback)
		{
			PutFileByName(BinaryStateFileNames.Input, callback);
		}

		public void PutMovieHeader(Action<Stream> callback)
		{
			PutFileByName(BinaryStateFileNames.Movieheader, callback);
		}
		*/

		private bool isDisposed;
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!isDisposed)
			{
				isDisposed = true;

				if (disposing)
				{
					zip.Dispose();
				}
			}
		}

	}
}
