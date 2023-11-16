using System;
using System.Runtime.InteropServices;

namespace NLua.Native
{
	internal static class DelegateExtensions
	{
		public static LuaNativeFunction ToLuaFunction(this IntPtr ptr)
			=> ptr == IntPtr.Zero ? null : Marshal.GetDelegateForFunctionPointer<LuaNativeFunction>(ptr);

		public static IntPtr ToFunctionPointer(this LuaNativeFunction d)
			=> d == null ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(d);

		public static IntPtr ToFunctionPointer(this LuaKFunction d)
			=> d == null ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(d);

		public static IntPtr ToFunctionPointer(this LuaReader d)
			=> d == null ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(d);

		public static IntPtr ToFunctionPointer(this LuaWriter d)
			=> d == null ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(d);

		public static LuaAlloc ToLuaAlloc(this IntPtr ptr)
			=> ptr == IntPtr.Zero ? null : Marshal.GetDelegateForFunctionPointer<LuaAlloc>(ptr);

		public static IntPtr ToFunctionPointer(this LuaAlloc d)
			=> d == null ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(d);
	}
}
