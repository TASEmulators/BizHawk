using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace BizHawk.Common
{
	/// <summary>
	/// Starts a thread which cleans any filenames in %temp% beginning with bizhawk.bizdelete.
	/// Files shouldn't be named that unless they're safe to delete, but notably, they may stil be in use. That won't hurt this component.
	/// When they're no longer in use, this component will then be able to delete them.
	/// </summary>
	public static class TempFileCleaner
	{
		// TODO - manage paths other than %temp%, make not static, or allow adding multiple paths to static instance

		public static string GetTempFilename(string friendlyname, string extension = null, bool delete = true)
		{
			string guidPart = Guid.NewGuid().ToString();
			var fname = $"biz-{System.Diagnostics.Process.GetCurrentProcess().Id}-{friendlyname}-{guidPart}{extension ?? ""}";
			if (delete)
			{
				fname = RenameTempFilenameForDelete(fname);
			}

			return Path.Combine(Path.GetTempPath(), fname);
		}

		public static string RenameTempFilenameForDelete(string path)
		{
			string filename = Path.GetFileName(path);
			string dir = Path.GetDirectoryName(path);
			if (!filename.StartsWith("biz-"))
			{
				throw new InvalidOperationException();
			}

			filename = "bizdelete-" + filename.Remove(0, 4);
			return Path.Combine(dir, filename);
		}

		public static void Start()
		{
			lock (typeof(TempFileCleaner))
			{
				if (thread != null)
				{
					return;
				}

				thread = new Thread(ThreadProc)
				{
					IsBackground = true,
					Priority = ThreadPriority.Lowest
				};
				thread.Start();
			}
		}

		#if WINDOWS
		[DllImport("kernel32.dll", EntryPoint = "DeleteFileW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
		static extern bool DeleteFileW([MarshalAs(UnmanagedType.LPWStr)]string lpFileName);
		#endif

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
						// SHUT. UP. THE. EXCEPTIONS.
						#if WINDOWS
						DeleteFileW(fi.FullName);
						#else
						fi.Delete();
						#endif
					}
					catch
					{
					}

					// try not to do more than one thing per frame
					Thread.Sleep(100);
				}

				// try not to slam the filesystem too hard, we dont want this to cause any hiccups
				Thread.Sleep(5000);
			}
		}

		public static void Stop()
		{
		}

		static Thread thread;
	}
}