using System;
using System.Runtime.InteropServices;

namespace BizHawk.Common
{
	public static class XlibImports
	{
		private const string LIB = "libX11";

		[DllImport(LIB)]
		public static extern IntPtr XOpenDisplay(string? display_name);
	}
}
