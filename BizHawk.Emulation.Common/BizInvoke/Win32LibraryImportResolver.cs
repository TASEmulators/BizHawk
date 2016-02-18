using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Common.BizInvoke
{
	public class Win32LibraryImportResolver : IImportResolver, IDisposable
	{
		private IntPtr _p;

		public Win32LibraryImportResolver(string dllName)
		{
			_p = Win32.LoadLibrary(dllName);
			if (_p == IntPtr.Zero)
				throw new InvalidOperationException("LoadLibrary returned NULL");
		}

		public IntPtr Resolve(string entryPoint)
		{
			return Win32.GetProcAddress(_p, entryPoint);
		}

		private void Free()
		{
			if (_p != IntPtr.Zero)
			{
				Win32.FreeLibrary(_p);
				_p = IntPtr.Zero;
			}
		}

		public void Dispose()
		{
			Free();
			GC.SuppressFinalize(this);
		}

		~Win32LibraryImportResolver()
		{
			Free();
		}

		private static class Win32
		{
			[DllImport("kernel32.dll")]
			public static extern IntPtr LoadLibrary(string dllToLoad);
			[DllImport("kernel32.dll")]
			public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);
			[DllImport("kernel32.dll")]
			public static extern bool FreeLibrary(IntPtr hModule);
		}
	}
}
