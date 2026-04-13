using System.Runtime.InteropServices;
using System.Text;

using BizHawk.Common;

namespace BizHawk.Client.Common.Filters
{
	public static unsafe class Librashader
	{
		private const CallingConvention CC = CallingConvention.Cdecl;
		private static bool _loaded = false;

		public static bool IsLoaded => _loaded;

		public static bool Load()
		{
			if (_loaded) return true;

			Util.DebugWriteLine("[librashader] Attempting to load librashader.dll...");
			try
			{
				libra_instance_abi_version();
				Util.DebugWriteLine("[librashader] Successfully loaded librashader.dll");
				_loaded = true;
				return true;
			}
			catch (DllNotFoundException)
			{
				Util.DebugWriteLine("[librashader] Failed to load librashader.dll");
				return false;
			}
		}

		[DllImport("librashader", CallingConvention = CC)]
		public static extern IntPtr libra_instance_abi_version();

		[DllImport("librashader", CallingConvention = CC)]
		public static extern IntPtr libra_instance_api_version();

		[DllImport("librashader", CallingConvention = CC)]
		public static extern IntPtr libra_preset_create(byte* filename, out IntPtr preset);

		[DllImport("librashader", CallingConvention = CC)]
		public static extern int libra_preset_free(ref IntPtr preset);

		[DllImport("librashader", CallingConvention = CC)]
		public static extern int libra_error_free(ref IntPtr error);

		[DllImport("librashader", CallingConvention = CC)]
		public static extern int libra_error_print(IntPtr error);

		[DllImport("librashader", CallingConvention = CC)]
		public static extern int libra_error_errno(IntPtr error);

		[DllImport("librashader", CallingConvention = CC)]
		public static extern IntPtr libra_gl_filter_chain_create(
			ref IntPtr preset,
			IntPtr loader,
			[In] ref filter_chain_gl_opt_t options,
			out IntPtr chain);

		[DllImport("librashader", CallingConvention = CC)]
		public static extern int libra_gl_filter_chain_frame(
			ref IntPtr chain,
			UIntPtr frame_count,
			libra_image_gl_t image,
			libra_image_gl_t output,
			IntPtr viewport,
			IntPtr mvp,
			IntPtr options);

		[DllImport("librashader", CallingConvention = CC)]
		public static extern int libra_gl_filter_chain_free(ref IntPtr chain);

		public static IntPtr PresetCreate(string filename, out IntPtr preset)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(filename + "\0");
			fixed (byte* ptr = bytes)
			{
				return libra_preset_create(ptr, out preset);
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
				disable_cache = false,
			};
		}
	}
}
