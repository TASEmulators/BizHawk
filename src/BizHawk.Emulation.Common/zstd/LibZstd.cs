using System;
using System.Runtime.InteropServices;

using BizHawk.BizInvoke;

namespace BizHawk.Emulation.Common
{
	public abstract class LibZstd
	{
		private const CallingConvention cc = CallingConvention.Cdecl;

		[BizImport(cc)]
		public abstract uint ZSTD_isError(ulong code);

		[BizImport(cc)]
		public abstract IntPtr ZSTD_getErrorName(ulong code);

		[BizImport(cc)]
		public abstract int ZSTD_minCLevel();

		[BizImport(cc)]
		public abstract int ZSTD_maxCLevel();

		[StructLayout(LayoutKind.Sequential)]
		public struct StreamBuffer
		{
			public IntPtr Ptr;
			public ulong Size;
			public ulong Pos;
		}

		[BizImport(cc)]
		public abstract IntPtr ZSTD_createCStream();

		[BizImport(cc)]
		public abstract ulong ZSTD_freeCStream(IntPtr zcs);

		[BizImport(cc)]
		public abstract ulong ZSTD_initCStream(IntPtr zcs, int compressionLevel);

		[BizImport(cc)]
		public abstract ulong ZSTD_compressStream(IntPtr zcs, ref StreamBuffer output, ref StreamBuffer input);

		[BizImport(cc)]
		public abstract ulong ZSTD_flushStream(IntPtr zcs, ref StreamBuffer output);

		[BizImport(cc)]
		public abstract ulong ZSTD_endStream(IntPtr zcs, ref StreamBuffer output);

		[BizImport(cc)]
		public abstract IntPtr ZSTD_createDStream();

		[BizImport(cc)]
		public abstract ulong ZSTD_freeDStream(IntPtr zds);

		[BizImport(cc)]
		public abstract ulong ZSTD_initDStream(IntPtr zds);

		[BizImport(cc)]
		public abstract ulong ZSTD_decompressStream(IntPtr zds, ref StreamBuffer output, ref StreamBuffer input);
	}
}
