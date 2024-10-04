using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace NLua.Native
{
	public static class LuaNativeMethodLoader
	{
		private interface INativeLibraryLoader
		{
			IntPtr LoadNativeLibrary(string path);
			void FreeNativeLibrary(IntPtr handle);
			IntPtr LoadFunctionPointer(IntPtr handle, string symbol);
		}

		private class Win32NativeLibraryLoader : INativeLibraryLoader
		{
			[DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
			private static extern IntPtr LoadLibraryW(string lpLibFileName);

			[DllImport("kernel32.dll", ExactSpelling = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			private static extern bool FreeLibrary(IntPtr hLibModule);

			[DllImport("kernel32.dll", ExactSpelling = true)]
			private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

			public IntPtr LoadNativeLibrary(string path) => LoadLibraryW(path);

			public void FreeNativeLibrary(IntPtr handle) => FreeLibrary(handle);

			public IntPtr LoadFunctionPointer(IntPtr handle, string symbol) => GetProcAddress(handle, symbol);
		}

		private class LinuxNativeLibraryLoader : INativeLibraryLoader
		{
			private const int RTLD_NOW = 2;
			private const int RTLD_GLOBAL = 0x100;

			[DllImport("libdl.so.2")]
			private static extern IntPtr dlopen(string fileName, int flags);

			[DllImport("libdl.so.2")]
			private static extern int dlclose(IntPtr handle);

			[DllImport("libdl.so.2")]
			private static extern IntPtr dlsym(IntPtr handle, string symbol);

			public IntPtr LoadNativeLibrary(string path) => dlopen(path, RTLD_NOW | RTLD_GLOBAL);

			public void FreeNativeLibrary(IntPtr handle) => _ = dlclose(handle);

			public IntPtr LoadFunctionPointer(IntPtr handle, string symbol) => dlsym(handle, symbol);
		}

		private class LibcNativeLibraryLoader : INativeLibraryLoader
		{
			private const int RTLD_NOW = 2;
			private const int RTLD_GLOBAL = 0x100;

			[DllImport("libc")]
			private static extern IntPtr dlopen(string fileName, int flags);

			[DllImport("libc")]
			private static extern int dlclose(IntPtr handle);

			[DllImport("libc")]
			private static extern IntPtr dlsym(IntPtr handle, string symbol);

			public IntPtr LoadNativeLibrary(string path) => dlopen(path, RTLD_NOW | RTLD_GLOBAL);

			public void FreeNativeLibrary(IntPtr handle) => _ = dlclose(handle);

			public IntPtr LoadFunctionPointer(IntPtr handle, string symbol) => dlsym(handle, symbol);
		}

		private static INativeLibraryLoader GetNativeLibraryLoader()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				return new Win32NativeLibraryLoader();
			}

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				return new LinuxNativeLibraryLoader();
			}

			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
			    RuntimeInformation.OSDescription.ToUpperInvariant().Contains("BSD"))
			{
				return new LibcNativeLibraryLoader();
			}

			throw new NotSupportedException("This OS does not support loading native libraries");
		}

		private static IEnumerable<string> NativeLuaLibraryPaths()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				return new[] { "lua54.dll" };
			}

			// Linux is tricky as we want to use the system lua library
			// but old (but not yet EOL) distros may not have lua 5.4
			// we can safely use lua 5.3 for our purposes, hope the
			// user's distro provides at least lua 5.3!
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
			    RuntimeInformation.OSDescription.ToUpperInvariant().Contains("BSD"))
			{
				return new[]
				{
					"liblua.so.5.4.6", "liblua.so.5.4", "liblua-5.4.so", "liblua5.4.so", "liblua5.4.so.0",
					"liblua.so.5.3.6", "liblua.so.5.3", "liblua-5.3.so", "liblua5.3.so", "liblua5.3.so.0",
				};
			}

			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				return new[] { "liblua54.dylib" };
			}

			throw new NotSupportedException("This OS does not support loading native lua.");
		}

		private static unsafe bool LoadNativeLuaMethods(Func<string, IntPtr> loadFunction, LuaNativeMethods nativeMethods)
		{
			nativeMethods.lua_atpanic = (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr>)loadFunction("lua_atpanic");
			if (nativeMethods.lua_atpanic == null) return false;
			nativeMethods.lua_checkstack = (delegate* unmanaged[Cdecl]<IntPtr, int, int>)loadFunction("lua_checkstack");
			if (nativeMethods.lua_checkstack == null) return false;
			nativeMethods.lua_close = (delegate* unmanaged[Cdecl]<IntPtr, void>)loadFunction("lua_close");
			if (nativeMethods.lua_close == null) return false;
			nativeMethods.lua_compare = (delegate* unmanaged[Cdecl]<IntPtr, int, int, int, int>)loadFunction("lua_compare");
			if (nativeMethods.lua_compare == null) return false;
			nativeMethods.lua_createtable = (delegate* unmanaged[Cdecl]<IntPtr, int, int, void>)loadFunction("lua_createtable");
			if (nativeMethods.lua_createtable == null) return false;
			nativeMethods.lua_error = (delegate* unmanaged[Cdecl]<IntPtr, int>)loadFunction("lua_error");
			if (nativeMethods.lua_error == null) return false;
			nativeMethods.lua_getfield = (delegate* unmanaged[Cdecl]<IntPtr, int, IntPtr, int>)loadFunction("lua_getfield");
			if (nativeMethods.lua_getfield == null) return false;
			nativeMethods.lua_getglobal = (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, int>)loadFunction("lua_getglobal");
			if (nativeMethods.lua_getglobal == null) return false;
			nativeMethods.lua_getmetatable = (delegate* unmanaged[Cdecl]<IntPtr, int, int>)loadFunction("lua_getmetatable");
			if (nativeMethods.lua_getmetatable == null) return false;
			nativeMethods.lua_gettable = (delegate* unmanaged[Cdecl]<IntPtr, int, int>)loadFunction("lua_gettable");
			if (nativeMethods.lua_gettable == null) return false;
			nativeMethods.lua_gettop = (delegate* unmanaged[Cdecl]<IntPtr, int>)loadFunction("lua_gettop");
			if (nativeMethods.lua_gettop == null) return false;
			nativeMethods.lua_isinteger = (delegate* unmanaged[Cdecl]<IntPtr, int, int>)loadFunction("lua_isinteger");
			if (nativeMethods.lua_isinteger == null) return false;
			nativeMethods.lua_isnumber = (delegate* unmanaged[Cdecl]<IntPtr, int, int>)loadFunction("lua_isnumber");
			if (nativeMethods.lua_isnumber == null) return false;
			nativeMethods.lua_isstring = (delegate* unmanaged[Cdecl]<IntPtr, int, int>)loadFunction("lua_isstring");
			if (nativeMethods.lua_isstring == null) return false;
			nativeMethods.lua_newthread = (delegate* unmanaged[Cdecl]<IntPtr, IntPtr>)loadFunction("lua_newthread");
			if (nativeMethods.lua_newthread == null) return false;
			if (nativeMethods.IsLua53)
			{
				nativeMethods.lua_newuserdata = (delegate* unmanaged[Cdecl]<IntPtr, UIntPtr, IntPtr>)loadFunction("lua_newuserdata");
				if (nativeMethods.lua_newuserdata == null) return false;
			}
			else
			{
				nativeMethods.lua_newuserdatauv = (delegate* unmanaged[Cdecl]<IntPtr, UIntPtr, int, IntPtr>)loadFunction("lua_newuserdatauv");
				if (nativeMethods.lua_newuserdatauv == null) return false;
			}
			nativeMethods.lua_next = (delegate* unmanaged[Cdecl]<IntPtr, int, int>)loadFunction("lua_next");
			if (nativeMethods.lua_next == null) return false;
			nativeMethods.lua_pcallk = (delegate* unmanaged[Cdecl]<IntPtr, int, int, int, IntPtr, IntPtr, int>)loadFunction("lua_pcallk");
			if (nativeMethods.lua_pcallk == null) return false;
			nativeMethods.lua_pushboolean = (delegate* unmanaged[Cdecl]<IntPtr, int, void>)loadFunction("lua_pushboolean");
			if (nativeMethods.lua_pushboolean == null) return false;
			nativeMethods.lua_pushcclosure = (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, int, void>)loadFunction("lua_pushcclosure");
			if (nativeMethods.lua_pushcclosure == null) return false;
			nativeMethods.lua_pushinteger = (delegate* unmanaged[Cdecl]<IntPtr, long, void>)loadFunction("lua_pushinteger");
			if (nativeMethods.lua_pushinteger == null) return false;
			nativeMethods.lua_pushlightuserdata = (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void>)loadFunction("lua_pushlightuserdata");
			if (nativeMethods.lua_pushlightuserdata == null) return false;
			nativeMethods.lua_pushlstring = (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, UIntPtr, IntPtr>)loadFunction("lua_pushlstring");
			if (nativeMethods.lua_pushlstring == null) return false;
			nativeMethods.lua_pushnil = (delegate* unmanaged[Cdecl]<IntPtr, void>)loadFunction("lua_pushnil");
			if (nativeMethods.lua_pushnil == null) return false;
			nativeMethods.lua_pushnumber = (delegate* unmanaged[Cdecl]<IntPtr, double, void>)loadFunction("lua_pushnumber");
			if (nativeMethods.lua_pushnumber == null) return false;
			nativeMethods.lua_pushthread = (delegate* unmanaged[Cdecl]<IntPtr, int>)loadFunction("lua_pushthread");
			if (nativeMethods.lua_pushthread == null) return false;
			nativeMethods.lua_pushvalue = (delegate* unmanaged[Cdecl]<IntPtr, int, void>)loadFunction("lua_pushvalue");
			if (nativeMethods.lua_pushvalue == null) return false;
			nativeMethods.lua_rawequal = (delegate* unmanaged[Cdecl]<IntPtr, int, int, int>)loadFunction("lua_rawequal");
			if (nativeMethods.lua_rawequal == null) return false;
			nativeMethods.lua_rawget = (delegate* unmanaged[Cdecl]<IntPtr, int, int>)loadFunction("lua_rawget");
			if (nativeMethods.lua_rawget == null) return false;
			nativeMethods.lua_rawgeti = (delegate* unmanaged[Cdecl]<IntPtr, int, long, int>)loadFunction("lua_rawgeti");
			if (nativeMethods.lua_rawgeti == null) return false;
			nativeMethods.lua_rawset = (delegate* unmanaged[Cdecl]<IntPtr, int, void>)loadFunction("lua_rawset");
			if (nativeMethods.lua_rawset == null) return false;
			nativeMethods.lua_rawseti = (delegate* unmanaged[Cdecl]<IntPtr, int, long, void>)loadFunction("lua_rawseti");
			if (nativeMethods.lua_rawseti == null) return false;
			if (nativeMethods.IsLua53)
			{
				nativeMethods.lua_resume_53 = (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, int, int>)loadFunction("lua_resume");
				if (nativeMethods.lua_resume_53 == null) return false;
			}
			else
			{
				nativeMethods.lua_resume_54 = (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, int, out int, int>)loadFunction("lua_resume");
				if (nativeMethods.lua_resume_54 == null) return false;
			}
			nativeMethods.lua_rotate = (delegate* unmanaged[Cdecl]<IntPtr, int, int, void>)loadFunction("lua_rotate");
			if (nativeMethods.lua_rotate == null) return false;
			nativeMethods.lua_setglobal = (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void>)loadFunction("lua_setglobal");
			if (nativeMethods.lua_setglobal == null) return false;
			nativeMethods.lua_setmetatable = (delegate* unmanaged[Cdecl]<IntPtr, int, void>)loadFunction("lua_setmetatable");
			if (nativeMethods.lua_setmetatable == null) return false;
			nativeMethods.lua_settable = (delegate* unmanaged[Cdecl]<IntPtr, int, void>)loadFunction("lua_settable");
			if (nativeMethods.lua_settable == null) return false;
			nativeMethods.lua_settop = (delegate* unmanaged[Cdecl]<IntPtr, int, void>)loadFunction("lua_settop");
			if (nativeMethods.lua_settop == null) return false;
			nativeMethods.lua_toboolean = (delegate* unmanaged[Cdecl]<IntPtr, int, int>)loadFunction("lua_toboolean");
			if (nativeMethods.lua_toboolean == null) return false;
			nativeMethods.lua_tointegerx = (delegate* unmanaged[Cdecl]<IntPtr, int, out int, long>)loadFunction("lua_tointegerx");
			if (nativeMethods.lua_tointegerx == null) return false;
			nativeMethods.lua_tolstring = (delegate* unmanaged[Cdecl]<IntPtr, int, out UIntPtr, IntPtr>)loadFunction("lua_tolstring");
			if (nativeMethods.lua_tolstring == null) return false;
			nativeMethods.lua_tonumberx = (delegate* unmanaged[Cdecl]<IntPtr, int, out int, double>)loadFunction("lua_tonumberx");
			if (nativeMethods.lua_tonumberx == null) return false;
			nativeMethods.lua_tothread = (delegate* unmanaged[Cdecl]<IntPtr, int, IntPtr>)loadFunction("lua_tothread");
			if (nativeMethods.lua_tothread == null) return false;
			nativeMethods.lua_touserdata = (delegate* unmanaged[Cdecl]<IntPtr, int, IntPtr>)loadFunction("lua_touserdata");
			if (nativeMethods.lua_touserdata == null) return false;
			nativeMethods.lua_type = (delegate* unmanaged[Cdecl]<IntPtr, int, int>)loadFunction("lua_type");
			if (nativeMethods.lua_type == null) return false;
			nativeMethods.lua_xmove = (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, int, void>)loadFunction("lua_xmove");
			if (nativeMethods.lua_xmove == null) return false;
			nativeMethods.lua_yieldk = (delegate* unmanaged[Cdecl]<IntPtr, int, IntPtr, IntPtr, int>)loadFunction("lua_yieldk");
			if (nativeMethods.lua_yieldk == null) return false;
			nativeMethods.luaL_getmetafield = (delegate* unmanaged[Cdecl]<IntPtr, int, IntPtr, int>)loadFunction("luaL_getmetafield");
			if (nativeMethods.luaL_getmetafield == null) return false;
			nativeMethods.luaL_loadbufferx = (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, UIntPtr, IntPtr, IntPtr, int>)loadFunction("luaL_loadbufferx");
			if (nativeMethods.luaL_loadbufferx == null) return false;
			nativeMethods.luaL_loadfilex = (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, int>)loadFunction("luaL_loadfilex");
			if (nativeMethods.luaL_loadfilex == null) return false;
			nativeMethods.luaL_newmetatable = (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, int>)loadFunction("luaL_newmetatable");
			if (nativeMethods.luaL_newmetatable == null) return false;
			nativeMethods.luaL_newstate = (delegate* unmanaged[Cdecl]<IntPtr>)loadFunction("luaL_newstate");
			if (nativeMethods.luaL_newstate == null) return false;
			nativeMethods.luaL_openlibs = (delegate* unmanaged[Cdecl]<IntPtr, void>)loadFunction("luaL_openlibs");
			if (nativeMethods.luaL_openlibs == null) return false;
			nativeMethods.luaL_ref = (delegate* unmanaged[Cdecl]<IntPtr, int, int>)loadFunction("luaL_ref");
			if (nativeMethods.luaL_ref == null) return false;
			nativeMethods.luaL_tolstring = (delegate* unmanaged[Cdecl]<IntPtr, int, out UIntPtr, IntPtr>)loadFunction("luaL_tolstring");
			if (nativeMethods.luaL_tolstring == null) return false;
			nativeMethods.luaL_unref = (delegate* unmanaged[Cdecl]<IntPtr, int, int, void>)loadFunction("luaL_unref");
			if (nativeMethods.luaL_unref == null) return false;
			nativeMethods.luaL_where = (delegate* unmanaged[Cdecl]<IntPtr, int, void>)loadFunction("luaL_where");
			// ReSharper disable once ConvertIfStatementToReturnStatement
			if (nativeMethods.luaL_where == null) return false;

			return true;
		}

		private static readonly Lazy<LuaNativeMethods> _nativeMethods = new(() =>
		{
			var nativeLibraryLoader = GetNativeLibraryLoader();
			foreach (var path in NativeLuaLibraryPaths())
			{
				var handle = nativeLibraryLoader.LoadNativeLibrary(path);
				if (handle == IntPtr.Zero)
				{
					continue;
				}

				var nativeMethods = new LuaNativeMethods { IsLua53 = path.IndexOf("5.3", StringComparison.Ordinal) != -1 };
				if (!LoadNativeLuaMethods(sym => nativeLibraryLoader.LoadFunctionPointer(handle, sym), nativeMethods))
				{
					nativeLibraryLoader.FreeNativeLibrary(handle);
					continue;
				}

				return nativeMethods;
			}

			throw new Exception("Could not load native lua methods");
		});

		internal static LuaNativeMethods GetNativeMethods()
			=> _nativeMethods.Value;

		public static bool EnsureNativeMethodsLoaded()
		{
			try
			{
				_ = GetNativeMethods();
				return true;
			}
			catch
			{
				return false;
			}
		}
	}
}
