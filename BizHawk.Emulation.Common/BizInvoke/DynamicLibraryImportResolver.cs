using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using BizHawk.Common;

namespace BizHawk.Emulation.Common.BizInvoke
{
	public class DynamicLibraryImportResolver : IImportResolver, IDisposable
	{
		private IntPtr _p;

		public DynamicLibraryImportResolver(string dllName)
		{
#if !MONO
			_p = Win32.LoadLibrary(dllName);
#else
			// TODO: how can we read name remaps out of app.confg <dllmap> ?
			_p = Libdl.dlopen(dllName, Libdl.RTLD_NOW);
#endif
			if (_p == IntPtr.Zero)
				throw new InvalidOperationException("LoadLibrary returned NULL");
		}

		public IntPtr Resolve(string entryPoint)
		{
#if !MONO
			return Win32.GetProcAddress(_p, entryPoint);
#else
			return Libdl.dlsym(_p, entryPoint);
#endif
		}

		private void Free()
		{
			if (_p != IntPtr.Zero)
			{
#if !MONO
				Win32.FreeLibrary(_p);
#else
				Libdl.dlclose(_p);
#endif
				_p = IntPtr.Zero;
			}
		}

		public void Dispose()
		{
			Free();
			GC.SuppressFinalize(this);
		}

		~DynamicLibraryImportResolver()
		{
			Free();
		}

#if !MONO
		private static class Win32
		{
			[DllImport("kernel32.dll")]
			public static extern IntPtr LoadLibrary(string dllToLoad);
			[DllImport("kernel32.dll")]
			public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);
			[DllImport("kernel32.dll")]
			public static extern bool FreeLibrary(IntPtr hModule);
		}
#else
		private static class Libdl
		{
			[DllImport("libdl.so")]
			public static extern IntPtr dlopen(string filename, int flags);
			[DllImport("libdl.so")]
			public static extern IntPtr dlsym(IntPtr handle, string symbol);
			[DllImport("libdl.so")]
			public static extern int dlclose(IntPtr handle);
			public const int RTLD_NOW = 2;
		}
#endif
	}
}
