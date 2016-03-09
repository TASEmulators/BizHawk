using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Common.BizInvoke
{
	public class DynamicLibraryImportResolver : IImportResolver, IDisposable
	{
		private IntPtr _p;

		public DynamicLibraryImportResolver(string dllName)
		{
#if WINDOWS
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
#if WINDOWS
			return Win32.GetProcAddress(_p, entryPoint);
#else
			return Libdl.dlsym(_p, entryPoint);
#endif
		}

		private void Free()
		{
			if (_p != IntPtr.Zero)
			{
#if WINDOWS
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

#if WINDOWS
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
			[DllImport("libc")] 
			static extern int uname(IntPtr buf); 
			static bool IsRunningOnMac(){
				//Borrowed from a forum post somewhere. Cannot use Environment.Platform because Mono returns Unix for both OSX and Unix.
				IntPtr buf = IntPtr.Zero; 
				try{ 
					buf = Marshal.AllocHGlobal(8192); 
					// This is a hacktastic way of getting sysname from uname () 
					if (uname(buf) == 0){ 
						string os = Marshal.PtrToStringAnsi(buf); 
						if (os == "Darwin")return true; 
					} 
				} 
				catch { } 
				finally{ 
					if (buf != IntPtr.Zero) Marshal.FreeHGlobal(buf); 
				} 
				return false; 
			}

			public static IntPtr dlopen(string filename, int flags){
				bool isMac = IsRunningOnMac();
				string realFile = System.IO.Path.GetFileNameWithoutExtension(filename);
				if (!realFile.StartsWith("lib"))
				{
					realFile = "lib" + realFile;
				}
				if (isMac)
				{
					realFile = realFile + ".dylib";
				}
				else
				{
					realFile = realFile + ".so";
				}
				realFile = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), realFile);
				return (isMac) ? dlMac.dlopen(realFile, flags) : dlLinux.dlopen(realFile, flags);
			}

			public static IntPtr dlsym(IntPtr handle, string symbol){
				return (IsRunningOnMac()) ? dlMac.dlsym(handle, symbol) : dlLinux.dlsym(handle, symbol);
			}

			public static int dlclose(IntPtr handle){
				return (IsRunningOnMac()) ? dlMac.dlclose(handle) : dlLinux.dlclose(handle);
			}

			public const int RTLD_NOW = 2;
			private static class dlMac
			{
				const string SystemLib = "/usr/lib/libSystem.dylib"; 
				[DllImport(SystemLib)]
				public static extern IntPtr dlopen(string filename, int flags);
				[DllImport(SystemLib)]
				public static extern IntPtr dlsym(IntPtr handle, string symbol);
				[DllImport(SystemLib)]
				public static extern int dlclose(IntPtr handle);
			}

			private static class dlLinux
			{
				const string SystemLib = "libdl.so"; 
				[DllImport(SystemLib)]
				public static extern IntPtr dlopen(string filename, int flags);
				[DllImport(SystemLib)]
				public static extern IntPtr dlsym(IntPtr handle, string symbol);
				[DllImport(SystemLib)]
				public static extern int dlclose(IntPtr handle);
			}
		}
#endif
	}
}
