using System;
using System.IO;
using System.Runtime.InteropServices;

namespace BizHawk.Common
{
	public class InstanceDll : IDisposable, IImportResolver
	{
		public InstanceDll(string dllPath)
		{
			//copy the dll to a temp directory
			var path = TempFileCleaner.GetTempFilename(string.Format("{0}", Path.GetFileNameWithoutExtension(dllPath)),".dll",false);
			using (var stream = new FileStream(path, FileMode.Create, System.Security.AccessControl.FileSystemRights.FullControl, FileShare.ReadWrite | FileShare.Delete, 4 * 1024, FileOptions.None))
			using (var sdll = File.OpenRead(dllPath))
				sdll.CopyTo(stream);

			//try to locate dlls in the current directory (for libretro cores)
			//this isnt foolproof but its a little better than nothing
			//setting PWD temporarily doesnt work. that'd be ideal since it supposedly gets searched early on,
			//but i guess not with SetDllDirectory in effect
			var envpath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);
			try
			{
				string envpath_new = Path.GetDirectoryName(path) + ";" + envpath;
				Environment.SetEnvironmentVariable("PATH", envpath_new, EnvironmentVariableTarget.Process);
				_hModule = LoadLibrary(path); //consider using LoadLibraryEx instead of shenanigans?
				var newfname = TempFileCleaner.RenameTempFilenameForDelete(path);
				File.Move(path, newfname);
			}
			finally
			{
				Environment.SetEnvironmentVariable("PATH", envpath, EnvironmentVariableTarget.Process);
			}
		}

		[Flags]
		enum LoadLibraryFlags : uint
		{
			DONT_RESOLVE_DLL_REFERENCES = 0x00000001,
			LOAD_IGNORE_CODE_AUTHZ_LEVEL = 0x00000010,
			LOAD_LIBRARY_AS_DATAFILE = 0x00000002,
			LOAD_LIBRARY_AS_DATAFILE_EXCLUSIVE = 0x00000040,
			LOAD_LIBRARY_AS_IMAGE_RESOURCE = 0x00000020,
			LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern IntPtr LoadLibrary(string dllToLoad);
		[DllImport("kernel32.dll", SetLastError = true)]
		static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hReservedNull, LoadLibraryFlags dwFlags);
		[DllImport("kernel32.dll")]
		static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);
		[DllImport("kernel32.dll")]
		static extern bool FreeLibrary(IntPtr hModule);

		public IntPtr GetProcAddress(string procName)
		{
			return GetProcAddress(_hModule, procName);
		}

		IntPtr IImportResolver.Resolve(string entryPoint)
		{
			return GetProcAddress(entryPoint);
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