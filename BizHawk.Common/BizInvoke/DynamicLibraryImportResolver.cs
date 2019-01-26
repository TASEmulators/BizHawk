using System;
using System.Runtime.InteropServices;

using BizHawk.Common;

namespace BizHawk.Common.BizInvoke
{
	public class DynamicLibraryImportResolver : IImportResolver, IDisposable
	{
		private IntPtr _p;
		private readonly PlatformLinkedLibSingleton.PlatformLinkedLibManager libLoader = PlatformLinkedLibSingleton.LinkedLibManager;

		public DynamicLibraryImportResolver(string dllName)
		{
			ResolveFilePath(ref dllName);
			_p = libLoader.LoadPlatformSpecific(dllName);
			if (_p == IntPtr.Zero) throw new InvalidOperationException("null pointer returned by LoadPlatformSpecific");
		}

		private string[] RelativeSearchPaths = new string[]
		{
			"/",
			"/dll/"			
		};

		private string[] AbsoluteSearchPaths = new string[]
		{
			"/usr/lib/",
			"/usr/lib/bizhawk/"
		};

		private void ResolveFilePath(ref string dllName)
		{
			if (PlatformLinkedLibSingleton.RunningOnUnix && !dllName.Contains("/"))
			{
				// not an absolute path and we are on linux
				// this is needed to actually find the DLL properly
				string currDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).Replace("file:", "");
				string dll = dllName;

				foreach (var p in AbsoluteSearchPaths)
				{
					dll = p + dllName;
					if (System.IO.File.Exists(dll))
					{
						dllName = dll;
						return;
					}
				}

				foreach (var p in RelativeSearchPaths)
				{
					dll = currDir + p + dllName;
					if (System.IO.File.Exists(dll))
					{
						dllName = dll;
						return;
					}
				}
			}
		}

		public IntPtr Resolve(string entryPoint)
		{
			return libLoader.GetProcAddr(_p, entryPoint);
		}

		private void Free()
		{
			if (_p == IntPtr.Zero) return;
			libLoader.FreePlatformSpecific(_p);
			_p = IntPtr.Zero;
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
	}
}
