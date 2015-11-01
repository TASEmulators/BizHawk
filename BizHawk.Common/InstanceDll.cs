using System;
using System.IO;
using System.Runtime.InteropServices;

namespace BizHawk.Common
{
	public class InstanceDll : IDisposable
	{
		public InstanceDll(string dllPath)
		{
			//copy the dll to a temp directory
			var path = Path.Combine(Path.GetTempPath(), "instancedll-pid" + System.Diagnostics.Process.GetCurrentProcess().Id + "-" + Guid.NewGuid()) + "-" + Path.GetFileName(dllPath);
			using (var stream = new FileStream(path, FileMode.Create, System.Security.AccessControl.FileSystemRights.FullControl, FileShare.ReadWrite | FileShare.Delete, 4 * 1024, FileOptions.None))
			using (var sdll = File.OpenRead(dllPath))
				sdll.CopyTo(stream);

			_hModule = LoadLibrary(path);
			var newfname = Path.GetFileName(path);
			newfname = "bizhawk.bizdelete-" + newfname;
			var newpath = Path.Combine(Path.GetDirectoryName(path), newfname);
			File.Move(path, newpath);
		}

		[DllImport("kernel32.dll")]
		static extern IntPtr LoadLibrary(string dllToLoad);
		[DllImport("kernel32.dll")]
		static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);
		[DllImport("kernel32.dll")]
		static extern bool FreeLibrary(IntPtr hModule);

		public IntPtr GetProcAddress(string procName)
		{
			return GetProcAddress(_hModule, procName);
		}

		public void Dispose()
		{
			if (_hModule != IntPtr.Zero)
			{
				FreeLibrary(_hModule);
				_hModule = IntPtr.Zero;
			}
		}

		IntPtr _hModule;
	}
}
