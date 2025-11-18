#nullable disable

#if AVI_SUPPORT
using System.Runtime.InteropServices;

using HRESULT = int;

// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace BizHawk.Common
{
	public static class AVIWriterImports
	{
		[Flags]
		public enum OpenFileStyle : uint
		{
			OF_WRITE = 0x00000001,
			OF_CREATE = 0x00001000,
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct AVISTREAMINFOW
		{
			public uint fccType;
			public uint fccHandler;
			public uint dwFlags;
			public uint dwCaps;
			public ushort wPriority;
			public ushort wLanguage;
			public uint dwScale;
			public uint dwRate;
			public uint dwStart;
			public uint dwLength;
			public uint dwInitialFrames;
			public uint dwSuggestedBufferSize;
			public uint dwQuality;
			public uint dwSampleSize;
			public RECT rcFrame;
			public uint dwEditCount;
			public uint dwFormatChangeCount;
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
			public uint fccType;
			public uint fccHandler;
			public uint dwKeyFrameEvery;
			public uint dwQuality;
			public uint dwBytesPerSecond;
			public uint dwFlags;
			public IntPtr lpFormat;
			public uint cbFormat;
			public IntPtr lpParms;
			public uint cbParms;
			public uint dwInterleaveEvery;
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
		public static extern HRESULT AVIFileCreateStreamW(IntPtr pfile, out IntPtr ppavi, in AVISTREAMINFOW psi);

		[DllImport("avifil32.dll", ExactSpelling = true)]
		public static extern void AVIFileInit();

		[DllImport("avifil32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		public static extern HRESULT AVIFileOpenW(out IntPtr ppfile, [MarshalAs(UnmanagedType.LPWStr)] string szFile, OpenFileStyle uMode, /*(Guid*)*/IntPtr lpHandler);

		/// <summary>Release an open AVI stream</summary>
		[DllImport("avifil32.dll", ExactSpelling = true)]
		public static extern uint AVIFileRelease(IntPtr pfile);

		/// <summary>Create a compressed stream from an uncompressed stream and a compression filter, and returns the address of a pointer to the compressed stream</summary>
		[DllImport("avifil32.dll", ExactSpelling = true)]
		public static extern HRESULT AVIMakeCompressedStream(out IntPtr ppsCompressed, IntPtr ppsSource, in AVICOMPRESSOPTIONS lpOptions, IntPtr pclsidHandler);

		/// <summary>Retrieve the save options for a file and returns them in a buffer</summary>
		[DllImport("avifil32.dll", ExactSpelling = true)]
		public static extern unsafe nint AVISaveOptions(IntPtr hwnd, uint uiFlags, int nStreams, void* ppavi, AVICOMPRESSOPTIONS** plpOptions);

		/// <inheritdoc cref="AVIFileRelease"/>
		[DllImport("avifil32.dll", ExactSpelling = true)]
		public static extern uint AVIStreamRelease(IntPtr pavi);

		/// <summary>Set the format of a stream at the specified position</summary>
		[DllImport("avifil32.dll", ExactSpelling = true)]
		public static extern HRESULT AVIStreamSetFormat(IntPtr pavi, int lPos, ref BITMAPINFOHEADER lpFormat, int cbFormat);

		/// <inheritdoc cref="AVIStreamSetFormat(System.IntPtr,int,ref BizHawk.Common.AVIWriterImports.BITMAPINFOHEADER,int)"/>
		[DllImport("avifil32.dll", ExactSpelling = true)]
		public static extern HRESULT AVIStreamSetFormat(IntPtr pavi, int lPos, ref WAVEFORMATEX lpFormat, int cbFormat);

		/// <summary>Write data to a stream</summary>
		[DllImport("avifil32.dll", ExactSpelling = true)]
		public static extern HRESULT AVIStreamWrite(IntPtr pavi, int lStart, int lSamples, IntPtr lpBuffer, int cbBuffer, uint dwFlags, /*out int*/IntPtr plSampWritten, out int plBytesWritten);
	}
}
#endif
