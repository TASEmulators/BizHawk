using System;
using System.Runtime.InteropServices;

using BizHawk.Common;

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
