using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace BizHawk.Common
{
	public class UnmanagedResourceHeap : IDisposable
	{
		public IntPtr StringToHGlobalAnsi(string str)
		{
			var ret = Marshal.StringToHGlobalAnsi(str);
			HGlobals.Add(ret);
			return ret;
		}

		public List<IntPtr> HGlobals = new List<IntPtr>();

		public void Dispose()
		{
			foreach (var h in HGlobals)
			{
				Marshal.FreeHGlobal(h);
			}

			HGlobals.Clear();
		}
	}
}
