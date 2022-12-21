using System;
using System.Runtime.InteropServices;

namespace NLua
{
	internal static class DelegateExtensions
	{
		public static LuaNativeFunction ToLuaFunction(this IntPtr ptr)
		{
			if (ptr == IntPtr.Zero)
				return null;

			return Marshal.GetDelegateForFunctionPointer<LuaNativeFunction>(ptr);
		}

		public static IntPtr ToFunctionPointer(this LuaNativeFunction d)
		{
			if (d == null)
				return IntPtr.Zero;

			return Marshal.GetFunctionPointerForDelegate<LuaNativeFunction>(d);
		}

		public static LuaHookFunction ToLuaHookFunction(this IntPtr ptr)
		{
			if (ptr == IntPtr.Zero)
				return null;

			return Marshal.GetDelegateForFunctionPointer<LuaHookFunction>(ptr);
		}

		public static IntPtr ToFunctionPointer(this LuaHookFunction d)
		{
			if (d == null)
				return IntPtr.Zero;

			return Marshal.GetFunctionPointerForDelegate<LuaHookFunction>(d);
		}

		public static LuaKFunction ToLuaKFunction(this IntPtr ptr)
		{
			if (ptr == IntPtr.Zero)
				return null;

			return Marshal.GetDelegateForFunctionPointer<LuaKFunction>(ptr);
		}

		public static IntPtr ToFunctionPointer(this LuaKFunction d)
		{
			if (d == null)
				return IntPtr.Zero;

			return Marshal.GetFunctionPointerForDelegate<LuaKFunction>(d);
		}

		public static LuaReader ToLuaReader(this IntPtr ptr)
		{
			if (ptr == IntPtr.Zero)
				return null;

			return Marshal.GetDelegateForFunctionPointer<LuaReader>(ptr);
		}

		public static IntPtr ToFunctionPointer(this LuaReader d)
		{
			if (d == null)
				return IntPtr.Zero;

			return Marshal.GetFunctionPointerForDelegate<LuaReader>(d);
		}

		public static LuaWriter ToLuaWriter(this IntPtr ptr)
		{
			if (ptr == IntPtr.Zero)
				return null;

			return Marshal.GetDelegateForFunctionPointer<LuaWriter>(ptr);
		}

		public static IntPtr ToFunctionPointer(this LuaWriter d)
		{
			if (d == null)
				return IntPtr.Zero;

			return Marshal.GetFunctionPointerForDelegate<LuaWriter>(d);
		}

		public static LuaAlloc ToLuaAlloc(this IntPtr ptr)
		{
			if (ptr == IntPtr.Zero)
				return null;

			return Marshal.GetDelegateForFunctionPointer<LuaAlloc>(ptr);
		}

		public static IntPtr ToFunctionPointer(this LuaAlloc d)
		{
			if (d == null)
				return IntPtr.Zero;

			return Marshal.GetFunctionPointerForDelegate<LuaAlloc>(d);
		}
	}
}
