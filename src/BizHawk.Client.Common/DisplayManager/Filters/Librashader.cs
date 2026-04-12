using System.Runtime.InteropServices;
using System.Text;

using BizHawk.Common;

namespace BizHawk.Client.Common.Filters
{
	public static unsafe class Librashader
	{
		private const string DllName = "librashader.dll";
		private static IntPtr _handle = IntPtr.Zero;
		private static bool _loaded = false;

		public static bool IsLoaded => _loaded;

		public static bool Load()
		{
			if (_loaded) return true;

			Util.DebugWriteLine("[librashader] Attempting to load dll\\librashader.dll...");
			_handle = LoadLibrary("dll\\librashader.dll");
			if (_handle == IntPtr.Zero)
			{
				Util.DebugWriteLine("[librashader] Failed to load librashader.dll");
				return false;
			}

			Util.DebugWriteLine("[librashader] Successfully loaded librashader.dll");
			_loaded = true;
			LoadFunctions();
			return true;
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr LoadLibrary(string lpFileName);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

		private static T GetDelegate<T>(string name) where T : Delegate
		{
			IntPtr addr = GetProcAddress(_handle, name);
			return addr != IntPtr.Zero ? Marshal.GetDelegateForFunctionPointer<T>(addr) : null;
		}

		public delegate IntPtr PFN_libra_instance_abi_version();
		public delegate IntPtr PFN_libra_instance_api_version();
		public delegate IntPtr PFN_libra_preset_create(byte* filename, out IntPtr preset);
		public delegate int PFN_libra_preset_free(ref IntPtr preset);
		public delegate int PFN_libra_error_free(ref IntPtr error);
		public delegate int PFN_libra_error_print(IntPtr error);
		public delegate int PFN_libra_error_errno(IntPtr error);

		public delegate IntPtr PFN_libra_gl_loader_t(byte* name);

		public delegate IntPtr PFN_libra_gl_filter_chain_create(
			ref IntPtr preset,
			IntPtr loader,
			[In] ref filter_chain_gl_opt_t options,
			out IntPtr chain);

		public delegate int PFN_libra_gl_filter_chain_frame(
			ref IntPtr chain,
			UIntPtr frame_count,
			libra_image_gl_t image,
			libra_image_gl_t output,
			IntPtr viewport,
			IntPtr mvp,
			IntPtr options);

		public delegate int PFN_libra_gl_filter_chain_free(ref IntPtr chain);

		private static PFN_libra_instance_abi_version _instance_abi_version;
		private static PFN_libra_instance_api_version _instance_api_version;
		private static PFN_libra_preset_create _preset_create;
		private static PFN_libra_preset_free _preset_free;
		private static PFN_libra_error_free _error_free;
		private static PFN_libra_error_print _error_print;
		private static PFN_libra_error_errno _error_errno;
		private static PFN_libra_gl_filter_chain_create _gl_filter_chain_create;
		private static PFN_libra_gl_filter_chain_frame _gl_filter_chain_frame;
		private static PFN_libra_gl_filter_chain_free _gl_filter_chain_free;

		internal static PFN_libra_preset_create preset_create => _preset_create;
		internal static PFN_libra_preset_free preset_free => _preset_free;
		internal static PFN_libra_error_print error_print => _error_print;
		internal static PFN_libra_gl_filter_chain_create gl_filter_chain_create => _gl_filter_chain_create;
		internal static PFN_libra_gl_filter_chain_frame gl_filter_chain_frame => _gl_filter_chain_frame;
		internal static PFN_libra_gl_filter_chain_free gl_filter_chain_free => _gl_filter_chain_free;

		private static void LoadFunctions()
		{
			_instance_abi_version = GetDelegate<PFN_libra_instance_abi_version>("libra_instance_abi_version");
			_instance_api_version = GetDelegate<PFN_libra_instance_api_version>("libra_instance_api_version");
			_preset_create = GetDelegate<PFN_libra_preset_create>("libra_preset_create");
			_preset_free = GetDelegate<PFN_libra_preset_free>("libra_preset_free");
			_error_free = GetDelegate<PFN_libra_error_free>("libra_error_free");
			_error_print = GetDelegate<PFN_libra_error_print>("libra_error_print");
			_error_errno = GetDelegate<PFN_libra_error_errno>("libra_error_errno");
			_gl_filter_chain_create = GetDelegate<PFN_libra_gl_filter_chain_create>("libra_gl_filter_chain_create");
			_gl_filter_chain_frame = GetDelegate<PFN_libra_gl_filter_chain_frame>("libra_gl_filter_chain_frame");
			_gl_filter_chain_free = GetDelegate<PFN_libra_gl_filter_chain_free>("libra_gl_filter_chain_free");
		}

		public static IntPtr PresetCreate(string filename, out IntPtr preset)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(filename + "\0");
			fixed (byte* ptr = bytes)
			{
				return _preset_create(ptr, out preset);
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct filter_chain_gl_opt_t
		{
			public UIntPtr version;
			public ushort glsl_version;
			[MarshalAs(UnmanagedType.U1)]
			public bool use_dsa;
			[MarshalAs(UnmanagedType.U1)]
			public bool force_no_mipmaps;
			[MarshalAs(UnmanagedType.U1)]
			public bool disable_cache;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct libra_image_gl_t
		{
			public uint handle;
			public uint format;
			public uint width;
			public uint height;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct libra_viewport_t
		{
			public float x;
			public float y;
			public uint width;
			public uint height;
		}

		public static filter_chain_gl_opt_t CreateDefaultOptions()
		{
			return new filter_chain_gl_opt_t
			{
				version = new UIntPtr(1),
				glsl_version = 330,
				use_dsa = false,
				force_no_mipmaps = false,
				disable_cache = false
			};
		}
	}
}
