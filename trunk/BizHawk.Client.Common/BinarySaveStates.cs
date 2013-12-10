using System;
using ICSharpCode.SharpZipLib.Zip;
using System.IO;

namespace BizHawk.Client.Common
{
	public class BinaryStateFileNames
	{
		public const string Versiontag = "BizState 1.0";
		public const string Corestate = "Core";
		public const string Framebuffer = "Framebuffer";
		public const string Input = "Input Log";
		public const string CorestateText = "CoreText";
		public const string Movieheader = "Header";
	}

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
					zip.Close();
				}
			}
		}

		ZipFile zip;
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
				ret.zip = new ZipFile(Filename);
				var e = ret.zip.GetEntry(BinaryStateFileNames.Versiontag);
				if (!ret.GetFileByName(BinaryStateFileNames.Versiontag, false, ret.ReadVersion))
				{
					ret.zip.Close();
					return null;
				}
				return ret;
			}
			catch (ZipException)
			{
				return null;
			}
		}

		bool GetFileByName(string Name, bool abort, Action<Stream> callback)
		{
			var e = zip.GetEntry(Name);
			if (e != null)
			{
				using (Stream zs = zip.GetInputStream(e))
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

		public void GetCoreState(Action<Stream> callbackBinary, Action<Stream> callbackText)
		{
			if (!GetFileByName(BinaryStateFileNames.Corestate, false, callbackBinary)
				&& !GetFileByName(BinaryStateFileNames.CorestateText, false, callbackText))
				throw new Exception("Couldn't find Binary or Text savestate");
		}

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
	}

	public class BinaryStateSaver : IDisposable
	{
		private readonly ZipOutputStream zip;

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
			zip = new ZipOutputStream(s)
				{
					IsStreamOwner = false,
					UseZip64 = UseZip64.Off
				};
			zip.SetLevel(0);

			PutFileByName(BinaryStateFileNames.Versiontag, WriteVersion);	
		}

		void PutFileByName(string Name, Action<Stream> callback)
		{
			var e = new ZipEntry(Name) {CompressionMethod = CompressionMethod.Stored};
			zip.PutNextEntry(e);
			callback(zip);
			zip.CloseEntry();
		}

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
					zip.Close();
				}
			}
		}

	}
}
