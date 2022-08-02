using System;
using System.Runtime.InteropServices;

using BizHawk.BizInvoke;
using BizHawk.Common;

namespace BizHawk.Client.Common.Zstd
{
	public abstract class LibZstd
	{
		private const CallingConvention cc = CallingConvention.Cdecl;

		[BizImport(cc)]
		public abstract ulong ZSTD_getFrameContentSize(byte[] src, ulong srcSize);

		[BizImport(cc)]
		public abstract ulong ZSTD_compressBound(ulong srcSize);

		[BizImport(cc)]
		public abstract uint ZSTD_isError(ulong code);

		[BizImport(cc)]
		public abstract IntPtr ZSTD_getErrorName(ulong code);

		[BizImport(cc)]
		public abstract IntPtr ZSTD_createCCtx();

		[BizImport(cc)]
		public abstract ulong ZSTD_freeCCtx(IntPtr cctx);

		[BizImport(cc)]
		public abstract ulong ZSTD_compressCCtx(IntPtr cctx, byte[] dst, ulong dstCapacity, byte[] src, ulong srcSize, int compressionLevel);

		[BizImport(cc)]
		public abstract IntPtr ZSTD_createDCtx();

		[BizImport(cc)]
		public abstract ulong ZSTD_freeDCtx(IntPtr dctx);

		[BizImport(cc)]
		public abstract ulong ZSTD_decompressDCtx(IntPtr dctx, byte[] dst, ulong dstCapacity, byte[] src, ulong srcSize);

		[StructLayout(LayoutKind.Sequential)]
		public struct Buffer
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
		public abstract ulong ZSTD_compressStream(IntPtr zcs, ref Buffer output, ref Buffer input);

		[BizImport(cc)]
		public abstract ulong ZSTD_flushStream(IntPtr zcs, ref Buffer output);

		[BizImport(cc)]
		public abstract ulong ZSTD_endStream(IntPtr zcs, ref Buffer output);

		[BizImport(cc)]
		public abstract IntPtr ZSTD_createDStream();

		[BizImport(cc)]
		public abstract ulong ZSTD_freeDStream(IntPtr zds);

		[BizImport(cc)]
		public abstract ulong ZSTD_initDStream(IntPtr zds);

		[BizImport(cc)]
		public abstract ulong ZSTD_decompressStream(IntPtr zds, ref Buffer output, ref Buffer input);
	}
}
