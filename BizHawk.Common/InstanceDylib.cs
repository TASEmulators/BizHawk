using System;
using System.IO;
using System.Runtime.InteropServices;

namespace BizHawk.Common
{
	public class InstanceDylib : IDisposable, IInstanceDll
	{
		public InstanceDylib(string dylibPath)
		{
			//copy the dll to a temp directory
			var path = TempFileCleaner.GetTempFilename(string.Format("{0}", Path.GetFileNameWithoutExtension(dylibPath)), ".dylib", false);
			using (var stream = new FileStream(path, FileMode.Create, System.Security.AccessControl.FileSystemRights.FullControl, FileShare.ReadWrite | FileShare.Delete, 4 * 1024, FileOptions.None))
			using (var sdll = File.OpenRead(dylibPath))
				sdll.CopyTo(stream);

			//try to locate dlls in the current directory (for libretro cores)
			//this isnt foolproof but its a little better than nothing
			//setting PWD temporarily doesnt work. that'd be ideal since it supposedly gets searched early on,
			//but i guess not with SetDllDirectory in effect
			//var envpath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);
			try
			{
				//string envpath_new = Path.GetDirectoryName(path) + ";" + envpath;
				//Environment.SetEnvironmentVariable("PATH", envpath_new, EnvironmentVariableTarget.Process);
				_hModule = dlopen(path, RTLD_NOW);
				var newfname = TempFileCleaner.RenameTempFilenameForDelete(path);
				File.Move(path, newfname);
			}
			finally
			{
				//Environment.SetEnvironmentVariable("PATH", envpath, EnvironmentVariableTarget.Process);
			}
		}

		const int RTLD_NOW = 2;

		[DllImport("libSystem.B.dylib")]
		private static extern IntPtr dlopen(string path, int mode);

		[DllImport("libSystem.B.dylib")]
		private static extern IntPtr dlsym(IntPtr handle, string symbol);

		[DllImport("libSystem.B.dylib")]
		private static extern int dlclose(IntPtr handle);

		[DllImport("libSystem.B.dylib")]
		private static extern IntPtr dlerror();

		public IntPtr GetProcAddress(string procName)
		{
			// clear previous errors if any
			dlerror();
			var res = dlsym(_hModule, procName);
			var errPtr = dlerror();
			if (errPtr != IntPtr.Zero) {
				throw new Exception("dlsym: " + Marshal.PtrToStringAnsi(errPtr));
			}
			return res;
		}

		public void Dispose()
		{
			if (_hModule != IntPtr.Zero)
			{
				dlclose(_hModule);
				_hModule = IntPtr.Zero;
			}
		}

		IntPtr _hModule;
	}
}