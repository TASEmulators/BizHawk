using System;
using System.Runtime.InteropServices;

namespace NLua.Native
{
	internal static class DelegateExtensions
	{
		public static IntPtr ToFunctionPointer(this LuaNativeFunction d)
			=> d == null ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(d);
	}
}
