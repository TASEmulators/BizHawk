using System;
using System.Runtime.InteropServices;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Collections.Generic;

using BizHawk;

namespace BizHawk.MultiClient
{

	/// <summary>
	/// accesses a shared library using LoadLibrary and GetProcAddress
	/// </summary>
	public class Win32LibAccessor : ILibAccessor
	{
		public Win32LibAccessor(string dllPath)
		{
			mDllHandle = LoadLibrary(dllPath);
			if (mDllHandle == IntPtr.Zero) return;
			IsOpen = true;
		}

		public bool IsOpen { get; private set; }

		IntPtr mDllHandle;

		IntPtr ILibAccessor.GetProcAddress(string name)
		{
			if (!IsOpen) throw new InvalidOperationException("dll was not opened, you can't get a symbol from it");
			IntPtr ret = GetProcAddress(mDllHandle, name);
			if (ret == IntPtr.Zero) throw new InvalidOperationException("symbol name was not found in dll!");
			return ret;
		}

		public void Dispose()
		{
			if (mDllHandle == IntPtr.Zero)
				FreeLibrary(mDllHandle);
			mDllHandle = IntPtr.Zero;
			IsOpen = false;
		}

		~Win32LibAccessor()
		{
			Dispose();
		}

		[DllImport("kernel32.dll")]
		public static extern IntPtr LoadLibrary(string dllToLoad);

		[DllImport("kernel32.dll")]
		public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

		[DllImport("kernel32.dll")]
		public static extern bool FreeLibrary(IntPtr hModule);
	}

}