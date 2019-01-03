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
			_p = libLoader.LoadPlatformSpecific(dllName);
			if (_p == IntPtr.Zero) throw new InvalidOperationException("null pointer returned by LoadPlatformSpecific");
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
