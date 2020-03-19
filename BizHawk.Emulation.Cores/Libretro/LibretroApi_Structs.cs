using System;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Libretro
{
	partial class LibretroApi
	{

		public struct retro_game_geometry
		{
			public uint base_width;
			public uint base_height;
			public uint max_width;
			public uint max_height;
			public float aspect_ratio;
		}


		public unsafe struct retro_system_info
		{
			public sbyte* library_name;
			public sbyte* library_version;
			public sbyte* valid_extensions;
			public bool need_fullpath;
			public bool block_extract;
			short _pad;
		}

		public struct retro_system_timing
		{
			public double fps;
			public double sample_rate;
		}

		public struct retro_system_av_info
		{
			public retro_game_geometry geometry;
			public retro_system_timing timing;
		}

		//untested
		public struct retro_perf_counter
		{
			public string ident;
			public ulong start;
			public ulong total;
			public ulong call_cnt;

			[MarshalAs(UnmanagedType.U1)]
			public bool registered;
		}

		//perf callbacks
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate long retro_perf_get_time_usec_t();
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate long retro_perf_get_counter_t();
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate ulong retro_get_cpu_features_t();
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void retro_perf_log_t();
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void retro_perf_register_t(ref retro_perf_counter counter);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void retro_perf_start_t(ref retro_perf_counter counter);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void retro_perf_stop_t(ref retro_perf_counter counter);

		public struct retro_perf_callback
		{
			public IntPtr get_time_usec;
			public IntPtr get_cpu_features;
			public IntPtr get_perf_counter;
			public IntPtr perf_register;
			public IntPtr perf_start;
			public IntPtr perf_stop;
			public IntPtr perf_log;
		}
	}
}
