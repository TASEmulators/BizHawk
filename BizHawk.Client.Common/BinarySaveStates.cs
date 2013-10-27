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
	}


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

		private BinaryStateLoader()
		{
		}

		public static BinaryStateLoader LoadAndDetect(string Filename)
		{
			BinaryStateLoader ret = new BinaryStateLoader();
			try
			{
				ret.zip = new ZipFile(Filename);
				var e = ret.zip.GetEntry(BinaryStateFileNames.Versiontag);
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
			GetFileByName(BinaryStateFileNames.Corestate, true, callback);
		}

		public bool GetFrameBuffer(Action<Stream> callback)
		{
			return GetFileByName(BinaryStateFileNames.Framebuffer, false, callback);
		}

		public void GetInputLogRequired(Action<Stream> callback)
		{
			GetFileByName(BinaryStateFileNames.Input, true, callback);
		}
	}

	public class BinaryStateSaver : IDisposable
	{
		private readonly ZipOutputStream zip;

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

			PutFileByName(BinaryStateFileNames.Versiontag, ss => { });	
		}

		void PutFileByName(string Name, Action<Stream> callback)
		{
			var e = new ZipEntry(Name) {CompressionMethod = CompressionMethod.Stored};
			zip.PutNextEntry(e);
			callback(zip);
			zip.CloseEntry();
		}

		public void PutCoreState(Action<Stream> callback)
		{
			PutFileByName(BinaryStateFileNames.Corestate, callback);
		}

		public void PutFrameBuffer(Action<Stream> callback)
		{
			PutFileByName(BinaryStateFileNames.Framebuffer, callback);
		}

		public void PutInputLog(Action<Stream> callback)
		{
			PutFileByName(BinaryStateFileNames.Input, callback);
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
