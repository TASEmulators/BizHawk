using System;
using System.IO;

namespace BizHawk.Common
{
	/// <summary>
	/// Starts a thread which cleans any filenames in %temp% beginning with bizhawk.bizdelete.
	/// Files shouldn't be named that unless they're safe to delete, but notably, they may stil be in use. That won't hurt this component.
	/// When they're no longer in use, this component will then be able to delete them.
	/// </summary>
	public static class TempFileCleaner
	{
		//todo - manage paths other than %temp%, make not static, or allow adding multiple paths to static instance

		public static string GetTempFilename(string friendlyname, string extension = null, bool delete = true)
		{
			string guidPart = Guid.NewGuid().ToString();
			var fname = string.Format("biz-{0}-{1}-{2}{3}", System.Diagnostics.Process.GetCurrentProcess().Id, friendlyname, guidPart, extension ?? "");
			if (delete) fname = RenameTempFilenameForDelete(fname);
			return Path.Combine(Path.GetTempPath(), fname);
		}

		public static string RenameTempFilenameForDelete(string path)
		{
			string filename = Path.GetFileName(path);
			string dir = Path.GetDirectoryName(path);
			if (!filename.StartsWith("biz-")) throw new InvalidOperationException();
			filename = "bizdelete-" + filename.Remove(0, 4);
			return Path.Combine(dir, filename);
		}

		public static void Start()
		{
			lock (typeof(TempFileCleaner))
			{
				if (thread != null)
					return;

				thread = new System.Threading.Thread(ThreadProc);
				thread.IsBackground = true;
				thread.Priority = System.Threading.ThreadPriority.Lowest;
				thread.Start();
			}
		}

		static void ThreadProc()
		{
			var di = new DirectoryInfo(Path.GetTempPath());
			for (;;)
			{
				var fis = di.GetFiles("bizdelete-*");
				foreach (var fi in fis)
				{
					try
					{
						fi.Delete();
					}
					catch
					{
					}

					//try not to do more than one thing per frame
					System.Threading.Thread.Sleep(100);
				}

				//try not to slam the filesystem too hard, we dont want this to cause any hiccups
				System.Threading.Thread.Sleep(5000);
			}
		}

		public static void Stop()
		{
		}

		static System.Threading.Thread thread;
	}
}