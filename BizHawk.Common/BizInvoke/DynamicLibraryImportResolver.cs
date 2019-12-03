using System;
using System.IO;
using System.Reflection;

namespace BizHawk.Common.BizInvoke
{
	/// TODO move this and all in IImportResolver.cs to OSTailoredCode.cs and refactor
	public class DynamicLibraryImportResolver : IImportResolver, IDisposable
	{
		private IntPtr _p;
		private readonly OSTailoredCode.ILinkedLibManager libLoader = OSTailoredCode.LinkedLibManager; //TODO inline?

		public DynamicLibraryImportResolver(string dllName)
		{
			_p = libLoader.LoadOrThrow(dllName);
		}

		private string[] RelativeSearchPaths = {
			"/",
			"/dll/"
		};

		private string[] AbsoluteSearchPaths = {
			"/usr/lib/",
			"/usr/lib/bizhawk/"
		};

		/// <remarks>this is needed to actually find the DLL properly on Unix</remarks>
		private void ResolveFilePath(ref string dllName)
		{
			if (dllName.IndexOf('/') != -1) return; // relative paths shouldn't contain '/'

			var currDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase).Replace("file:", "");
			string dll;
			foreach (var p in AbsoluteSearchPaths)
			{
				dll = p + dllName;
				if (File.Exists(dll))
				{
					dllName = dll;
					return;
				}
			}
			foreach (var p in RelativeSearchPaths)
			{
				dll = currDir + p + dllName;
				if (File.Exists(dll))
				{
					dllName = dll;
					return;
				}
			}
		}

		public IntPtr Resolve(string entryPoint)
		{
			return libLoader.GetProcAddr(_p, entryPoint);
		}

		private void Free()
		{
			if (_p == IntPtr.Zero) return; // already freed
			libLoader.FreeByPtr(_p);
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
