#nullable disable

#if AVI_SUPPORT
using System.Runtime.InteropServices;

// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace BizHawk.Common
{
	public static class AVIWriterImports
	{
		[Flags]
		public enum OpenFileStyle : uint
		{
			OF_WRITE = 0x00000001,
			OF_CREATE = 0x00001000
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct AVISTREAMINFOW
		{
			public int fccType;
			public int fccHandler;
			public int dwFlags;
			public int dwCaps;
			public short wPriority;
			public short wLanguage;
			public int dwScale;
			public int dwRate;
			public int dwStart;
			public int dwLength;
			public int dwInitialFrames;
			public int dwSuggestedBufferSize;
			public int dwQuality;
			public int dwSampleSize;
			public RECT rcFrame;
			public int dwEditCount;
			public int dwFormatChangeCount;
			[MarshalAs(UnmanagedType.LPWStr, SizeConst = 64)]
			public string szName;

			[StructLayout(LayoutKind.Sequential, Pack = 1)]
			public struct RECT
			{
				public int Left;
				public int Top;
				public int Right;
				public int Bottom;
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct BITMAPINFOHEADER
		{
			public uint biSize;
			public int biWidth;
			public int biHeight;
			public ushort biPlanes;
			public ushort biBitCount;
			public uint biCompression;
			public uint biSizeImage;
			public int biXPelsPerMeter;
			public int biYPelsPerMeter;
			public uint biClrUsed;
			public uint biClrImportant;

			public void Init()
				=> biSize = (uint)Marshal.SizeOf(this);
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct AVICOMPRESSOPTIONS
		{
			public int fccType;
			public int fccHandler;
			public int dwKeyFrameEvery;
			public int dwQuality;
			public int dwBytesPerSecond;
			public int dwFlags;
			public IntPtr lpFormat;
			public int cbFormat;
			public IntPtr lpParms;
			public int cbParms;
			public int dwInterleaveEvery;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct WAVEFORMATEX
		{
			public ushort wFormatTag;
			public ushort nChannels;
			public uint nSamplesPerSec;
			public uint nAvgBytesPerSec;
			public ushort nBlockAlign;
			public ushort wBitsPerSample;
			public ushort cbSize;

			public void Init()
				=> cbSize = (ushort)Marshal.SizeOf(this);
		}

		/// <summary>Create a new stream in an existing file and creates an interface to the new stream</summary>
		[DllImport("avifil32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
		public static extern int AVIFileCreateStreamW(IntPtr pfile, out IntPtr ppavi, ref AVISTREAMINFOW psi);

		[DllImport("avifil32.dll", ExactSpelling = true)]
		public static extern void AVIFileInit();

		[DllImport("avifil32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		public static extern int AVIFileOpenW(ref IntPtr pAviFile, [MarshalAs(UnmanagedType.LPWStr)] string szFile, OpenFileStyle uMode, int lpHandler);

		/// <summary>Release an open AVI stream</summary>
		[DllImport("avifil32.dll", ExactSpelling = true)]
		public static extern int AVIFileRelease(IntPtr pfile);

		/// <summary>Create a compressed stream from an uncompressed stream and a compression filter, and returns the address of a pointer to the compressed stream</summary>
		[DllImport("avifil32.dll", ExactSpelling = true)]
		public static extern int AVIMakeCompressedStream(out IntPtr ppsCompressed, IntPtr psSource, ref AVICOMPRESSOPTIONS lpOptions, IntPtr pclsidHandler);

		/// <summary>Retrieve the save options for a file and returns them in a buffer</summary>
		[DllImport("avifil32.dll", ExactSpelling = true)]
		public static extern unsafe int AVISaveOptions(IntPtr hwnd, int flags, int streams, void* ppAvi, void* plpOptions);

		/// <inheritdoc cref="AVIFileRelease"/>
		[DllImport("avifil32.dll", ExactSpelling = true)]
		public static extern int AVIStreamRelease(IntPtr pavi);

		/// <summary>Set the format of a stream at the specified position</summary>
		[DllImport("avifil32.dll", ExactSpelling = true)]
		public static extern int AVIStreamSetFormat(IntPtr pavi, int lPos, ref BITMAPINFOHEADER lpFormat, int cbFormat);

		/// <inheritdoc cref="AVIStreamSetFormat(System.IntPtr,int,ref BizHawk.Common.AVIWriterImports.BITMAPINFOHEADER,int)"/>
		[DllImport("avifil32.dll", ExactSpelling = true)]
		public static extern int AVIStreamSetFormat(IntPtr pavi, int lPos, ref WAVEFORMATEX lpFormat, int cbFormat);

		/// <summary>Write data to a stream</summary>
		[DllImport("avifil32.dll", ExactSpelling = true)]
		public static extern int AVIStreamWrite(IntPtr pavi, int lStart, int lSamples, IntPtr lpBuffer, int cbBuffer, int dwFlags, IntPtr plSampWritten, out int plBytesWritten);
	}
}
#endif
