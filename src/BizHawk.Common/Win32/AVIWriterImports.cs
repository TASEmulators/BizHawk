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
			OF_CREATE = 0x00001000,
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
	}
}
#endif
