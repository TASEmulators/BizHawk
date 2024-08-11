using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BizHawk.Common
{
	public static unsafe class LibZstd
	{
		static LibZstd()
		{
			var resolver = new DynamicLibraryImportResolver(
				OSTailoredCode.IsUnixHost ? "libzstd.so.1" : "libzstd.dll", hasLimitedLifetime: false);
			ZSTD_isError = (delegate* unmanaged[Cdecl]<nuint, uint>)resolver.GetProcAddrOrThrow(nameof(ZSTD_isError));
			ZSTD_getErrorName = (delegate* unmanaged[Cdecl]<nuint, IntPtr>)resolver.GetProcAddrOrThrow(nameof(ZSTD_getErrorName));
			ZSTD_minCLevel = (delegate* unmanaged[Cdecl]<int>)resolver.GetProcAddrOrThrow(nameof(ZSTD_minCLevel));
			ZSTD_maxCLevel = (delegate* unmanaged[Cdecl]<int>)resolver.GetProcAddrOrThrow(nameof(ZSTD_maxCLevel));
			ZSTD_createCStream = (delegate* unmanaged[Cdecl]<IntPtr>)resolver.GetProcAddrOrThrow(nameof(ZSTD_createCStream));
			ZSTD_freeCStream = (delegate* unmanaged[Cdecl]<IntPtr, nuint>)resolver.GetProcAddrOrThrow(nameof(ZSTD_freeCStream));
			ZSTD_initCStream = (delegate* unmanaged[Cdecl]<IntPtr, int, nuint>)resolver.GetProcAddrOrThrow(nameof(ZSTD_initCStream));
			ZSTD_compressStream = (delegate* unmanaged[Cdecl]<IntPtr, StreamBuffer*, StreamBuffer*, nuint>)resolver.GetProcAddrOrThrow(nameof(ZSTD_compressStream));
			ZSTD_flushStream = (delegate* unmanaged[Cdecl]<IntPtr, StreamBuffer*, nuint>)resolver.GetProcAddrOrThrow(nameof(ZSTD_flushStream));
			ZSTD_endStream = (delegate* unmanaged[Cdecl]<IntPtr, StreamBuffer*, nuint>)resolver.GetProcAddrOrThrow(nameof(ZSTD_endStream));
			ZSTD_createDStream = (delegate* unmanaged[Cdecl]<IntPtr>)resolver.GetProcAddrOrThrow(nameof(ZSTD_createDStream));
			ZSTD_freeDStream = (delegate* unmanaged[Cdecl]<IntPtr, nuint>)resolver.GetProcAddrOrThrow(nameof(ZSTD_freeDStream));
			ZSTD_initDStream = (delegate* unmanaged[Cdecl]<IntPtr, nuint>)resolver.GetProcAddrOrThrow(nameof(ZSTD_initDStream));
			ZSTD_decompressStream = (delegate* unmanaged[Cdecl]<IntPtr, StreamBuffer*, StreamBuffer*, nuint>)resolver.GetProcAddrOrThrow(nameof(ZSTD_decompressStream));
		}

		private static readonly delegate* unmanaged[Cdecl]<nuint, uint> ZSTD_isError;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint IsError(nuint code) => ZSTD_isError(code);

		private static readonly delegate* unmanaged[Cdecl]<nuint, IntPtr> ZSTD_getErrorName;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IntPtr GetErrorName(nuint code) => ZSTD_getErrorName(code);

		private static readonly delegate* unmanaged[Cdecl]<int> ZSTD_minCLevel;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int MinCLevel() => ZSTD_minCLevel();

		private static readonly delegate* unmanaged[Cdecl]<int> ZSTD_maxCLevel;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int MaxCLevel() => ZSTD_maxCLevel();

		[StructLayout(LayoutKind.Sequential)]
		public struct StreamBuffer
		{
			public IntPtr Ptr;
			public nuint Size;
			public nuint Pos;
		}

		private static readonly delegate* unmanaged[Cdecl]<IntPtr> ZSTD_createCStream;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IntPtr CreateCStream() => ZSTD_createCStream();

		private static readonly delegate* unmanaged[Cdecl]<IntPtr, nuint> ZSTD_freeCStream;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static nuint FreeCStream(IntPtr zcs) => ZSTD_freeCStream(zcs);

		private static readonly delegate* unmanaged[Cdecl]<IntPtr, int, nuint> ZSTD_initCStream;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static nuint InitCStream(IntPtr zcs, int compressionLevel) => ZSTD_initCStream(zcs, compressionLevel);

		private static readonly delegate* unmanaged[Cdecl]<IntPtr, StreamBuffer*, StreamBuffer*, nuint> ZSTD_compressStream;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static nuint CompressStream(IntPtr zcs, ref StreamBuffer output, ref StreamBuffer input)
		{
			fixed (StreamBuffer* outputPtr = &output, inputPtr = &input)
			{
				return ZSTD_compressStream(zcs, outputPtr, inputPtr);
			}
		}

		private static readonly delegate* unmanaged[Cdecl]<IntPtr, StreamBuffer*, nuint> ZSTD_flushStream;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static nuint FlushStream(IntPtr zcs, ref StreamBuffer output)
		{
			fixed (StreamBuffer* outputPtr = &output)
			{
				return ZSTD_flushStream(zcs, outputPtr);
			}
		}

		private static readonly delegate* unmanaged[Cdecl]<IntPtr, StreamBuffer*, nuint> ZSTD_endStream;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static nuint EndStream(IntPtr zcs, ref StreamBuffer output)
		{
			fixed (StreamBuffer* outputPtr = &output)
			{
				return ZSTD_endStream(zcs, outputPtr);
			}
		}

		private static readonly delegate* unmanaged[Cdecl]<IntPtr> ZSTD_createDStream;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IntPtr CreateDStream() => ZSTD_createDStream();

		private static readonly delegate* unmanaged[Cdecl]<IntPtr, nuint> ZSTD_freeDStream;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static nuint FreeDStream(IntPtr zds) => ZSTD_freeDStream(zds);

		private static readonly delegate* unmanaged[Cdecl]<IntPtr, nuint> ZSTD_initDStream;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static nuint InitDStream(IntPtr zds) => ZSTD_initDStream(zds);

		private static readonly delegate* unmanaged[Cdecl]<IntPtr, StreamBuffer*, StreamBuffer*, nuint> ZSTD_decompressStream;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static nuint DecompressStream(IntPtr zds, ref StreamBuffer output, ref StreamBuffer input)
		{
			fixed (StreamBuffer* outputPtr = &output, inputPtr = &input)
			{
				return ZSTD_decompressStream(zds, outputPtr, inputPtr);
			}
		}
	}
}
