using System;
using System.IO;
using System.Reflection;

namespace BizHawk.Common.BizInvoke
{
	public class DynamicLibraryImportResolver : IImportResolver, IDisposable
	{
		private IntPtr _p;
		private readonly OSTailoredCode.ILinkedLibManager libLoader = OSTailoredCode.LinkedLibManager;

		public DynamicLibraryImportResolver(string dllName)
		{
			if (OSTailoredCode.CurrentOS != OSTailoredCode.DistinctOS.Windows) ResolveFilePath(ref dllName);
			_p = libLoader.LoadPlatformSpecific(dllName);
			if (_p == IntPtr.Zero) throw new InvalidOperationException($"null pointer returned by {nameof(libLoader.LoadPlatformSpecific)}");
		}

		private string[] SearchPaths = new[]
		{
			"/",
			"/dll/"
		};

		/// <remarks>this is needed to actually find the DLL properly on Unix</remarks>
		private void ResolveFilePath(ref string dllName)
		{
			if (dllName.IndexOf('/') != -1) return; // relative paths shouldn't contain '/'

			var currDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase).Replace("file:", "");
			var dll = dllName;
			foreach (var p in SearchPaths)
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
