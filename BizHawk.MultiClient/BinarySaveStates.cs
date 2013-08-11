using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using System.IO;

namespace BizHawk.MultiClient
{
	public class BinaryStateFileNames
	{
		public const string versiontag = "BizState 1.0";
		public const string corestate = "Core";
		public const string framebuffer = "Framebuffer";
		public const string input = "Input Log";
	}


	public class BinaryStateLoader : IDisposable
	{
		bool disposed = false;
		public void Dispose()
		{
			if (!disposed)
			{
				disposed = true;
				zip.Close();
			}
		}

		ZipFile zip;

		private BinaryStateLoader()
		{
		}

		public static BinaryStateLoader LoadAndDetect(string Filename)
		{
			BinaryStateLoader ret = new BinaryStateLoader();
			try
			{
				ret.zip = new ZipFile(Filename);
				var e = ret.zip.GetEntry(BinaryStateFileNames.versiontag);
				if (e == null)
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

		public void GetCoreState(Action<Stream> callback)
		{
			GetFileByName(BinaryStateFileNames.corestate, true, callback);
		}

		public bool GetFrameBuffer(Action<Stream> callback)
		{
			return GetFileByName(BinaryStateFileNames.framebuffer, false, callback);
		}
	}

	public class BinaryStateSaver : IDisposable
	{
		ZipOutputStream zip;

		bool disposed = false;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="s">not closed when finished!</param>
		public BinaryStateSaver(Stream s)
		{
			zip = new ZipOutputStream(s);
			zip.IsStreamOwner = false;
			zip.SetLevel(0);
			zip.UseZip64 = UseZip64.Off;

			PutFileByName(BinaryStateFileNames.versiontag, (ss) => { });	
		}

		void PutFileByName(string Name, Action<Stream> callback)
		{
			var e = new ZipEntry(Name);
			e.CompressionMethod = CompressionMethod.Stored;
			zip.PutNextEntry(e);
			callback(zip);
			zip.CloseEntry();
		}

		public void PutCoreState(Action<Stream> callback)
		{
			PutFileByName(BinaryStateFileNames.corestate, callback);
		}

		public void PutFrameBuffer(Action<Stream> callback)
		{
			PutFileByName(BinaryStateFileNames.framebuffer, callback);
		}

		public void PutInputLog(Action<Stream> callback)
		{
			PutFileByName(BinaryStateFileNames.input, callback);
		}

		public void Dispose()
		{
			if (!disposed)
			{
				disposed = true;
				zip.Dispose();
			}
		}

	}
}
