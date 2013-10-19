using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace BizHawk
{
	public static class Win32
	{

		public static bool Is64BitProcess { get { return (IntPtr.Size == 8); } }
		public static bool Is64BitOperatingSystem { get { return Is64BitProcess || InternalCheckIsWow64(); } }

		[DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool IsWow64Process(
				[In] IntPtr hProcess,
				[Out] out bool wow64Process
		);

		static bool InternalCheckIsWow64()
		{
			if ((Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 1) ||
					Environment.OSVersion.Version.Major >= 6)
			{
				using (var p = System.Diagnostics.Process.GetCurrentProcess())
				{
					bool retVal;
					if (!IsWow64Process(p.Handle, out retVal))
					{
						return false;
					}
					return retVal;
				}
			}
			else
			{
				return false;
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct RECT
		{
			private int _Left;
			private int _Top;
			private int _Right;
			private int _Bottom;

			public RECT(RECT Rectangle)
				: this(Rectangle.Left, Rectangle.Top, Rectangle.Right, Rectangle.Bottom)
			{
			}
			public RECT(int Left, int Top, int Right, int Bottom)
			{
				_Left = Left;
				_Top = Top;
				_Right = Right;
				_Bottom = Bottom;
			}

			public int X
			{
				get { return _Left; }
				set { _Left = value; }
			}
			public int Y
			{
				get { return _Top; }
				set { _Top = value; }
			}
			public int Left
			{
				get { return _Left; }
				set { _Left = value; }
			}
			public int Top
			{
				get { return _Top; }
				set { _Top = value; }
			}
			public int Right
			{
				get { return _Right; }
				set { _Right = value; }
			}
			public int Bottom
			{
				get { return _Bottom; }
				set { _Bottom = value; }
			}
			public int Height
			{
				get { return _Bottom - _Top; }
				set { _Bottom = value - _Top; }
			}
			public int Width
			{
				get { return _Right - _Left; }
				set { _Right = value + _Left; }
			}
			public Point Location
			{
				get { return new Point(Left, Top); }
				set
				{
					_Left = value.X;
					_Top = value.Y;
				}
			}
			public Size Size
			{
				get { return new Size(Width, Height); }
				set
				{
					_Right = value.Width + _Left;
					_Bottom = value.Height + _Top;
				}
			}

			public static implicit operator Rectangle(RECT Rectangle)
			{
				return new Rectangle(Rectangle.Left, Rectangle.Top, Rectangle.Width, Rectangle.Height);
			}
			public static implicit operator RECT(Rectangle Rectangle)
			{
				return new RECT(Rectangle.Left, Rectangle.Top, Rectangle.Right, Rectangle.Bottom);
			}
			public static bool operator ==(RECT Rectangle1, RECT Rectangle2)
			{
				return Rectangle1.Equals(Rectangle2);
			}
			public static bool operator !=(RECT Rectangle1, RECT Rectangle2)
			{
				return !Rectangle1.Equals(Rectangle2);
			}

			public override string ToString()
			{
				return "{Left: " + _Left + "; " + "Top: " + _Top + "; Right: " + _Right + "; Bottom: " + _Bottom + "}";
			}

			public override int GetHashCode()
			{
				return ToString().GetHashCode();
			}

			public bool Equals(RECT Rectangle)
			{
				return Rectangle.Left == _Left && Rectangle.Top == _Top && Rectangle.Right == _Right && Rectangle.Bottom == _Bottom;
			}

			public override bool Equals(object Object)
			{
				if (Object is RECT)
				{
					return Equals((RECT)Object);
				}
				else if (Object is Rectangle)
				{
					return Equals(new RECT((Rectangle)Object));
				}

				return false;
			}
		}
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct AVISTREAMINFOW
		{
			public Int32 fccType;
			public Int32 fccHandler;
			public Int32 dwFlags;
			public Int32 dwCaps;
			public Int16 wPriority;
			public Int16 wLanguage;
			public Int32 dwScale;
			public Int32 dwRate;
			public Int32 dwStart;
			public Int32 dwLength;
			public Int32 dwInitialFrames;
			public Int32 dwSuggestedBufferSize;
			public Int32 dwQuality;
			public Int32 dwSampleSize;
			public RECT rcFrame;
			public Int32 dwEditCount;
			public Int32 dwFormatChangeCount;
			[MarshalAs(UnmanagedType.LPWStr, SizeConst=64)]
			public string szName;
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
			{
				biSize = (uint)Marshal.SizeOf(this);
			}
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
			{
				cbSize = (ushort)Marshal.SizeOf(this);
			}
		}

		public const int WAVE_FORMAT_PCM = 1;
		public const int AVIIF_KEYFRAME = 0x00000010;


		[Flags]
		public enum OpenFileStyle : uint
		{
			OF_CANCEL = 0x00000800,  // Ignored. For a dialog box with a Cancel button, use OF_PROMPT.
			OF_CREATE = 0x00001000,  // Creates a new file. If file exists, it is truncated to zero (0) length.
			OF_DELETE = 0x00000200,  // Deletes a file.
			OF_EXIST = 0x00004000,  // Opens a file and then closes it. Used to test that a file exists
			OF_PARSE = 0x00000100,  // Fills the OFSTRUCT structure, but does not do anything else.
			OF_PROMPT = 0x00002000,  // Displays a dialog box if a requested file does not exist
			OF_READ = 0x00000000,  // Opens a file for reading only.
			OF_READWRITE = 0x00000002,  // Opens a file with read/write permissions.
			OF_REOPEN = 0x00008000,  // Opens a file by using information in the reopen buffer.

			// For MS-DOS–based file systems, opens a file with compatibility mode, allows any process on a
			// specified computer to open the file any number of times.
			// Other efforts to open a file with other sharing modes fail. This flag is mapped to the
			// FILE_SHARE_READ|FILE_SHARE_WRITE flags of the CreateFile function.
			OF_SHARE_COMPAT = 0x00000000,

			// Opens a file without denying read or write access to other processes.
			// On MS-DOS-based file systems, if the file has been opened in compatibility mode
			// by any other process, the function fails.
			// This flag is mapped to the FILE_SHARE_READ|FILE_SHARE_WRITE flags of the CreateFile function.
			OF_SHARE_DENY_NONE = 0x00000040,

			// Opens a file and denies read access to other processes.
			// On MS-DOS-based file systems, if the file has been opened in compatibility mode,
			// or for read access by any other process, the function fails.
			// This flag is mapped to the FILE_SHARE_WRITE flag of the CreateFile function.
			OF_SHARE_DENY_READ = 0x00000030,

			// Opens a file and denies write access to other processes.
			// On MS-DOS-based file systems, if a file has been opened in compatibility mode,
			// or for write access by any other process, the function fails.
			// This flag is mapped to the FILE_SHARE_READ flag of the CreateFile function.
			OF_SHARE_DENY_WRITE = 0x00000020,

			// Opens a file with exclusive mode, and denies both read/write access to other processes.
			// If a file has been opened in any other mode for read/write access, even by the current process,
			// the function fails.
			OF_SHARE_EXCLUSIVE = 0x00000010,

			// Verifies that the date and time of a file are the same as when it was opened previously.
			// This is useful as an extra check for read-only files.
			OF_VERIFY = 0x00000400,

			// Opens a file for write access only.
			OF_WRITE = 0x00000001
		}

		[DllImport("avifil32.dll", SetLastError = true)]
		public static extern int AVIFileOpenW(ref IntPtr pAviFile, [MarshalAs(UnmanagedType.LPWStr)] string szFile, OpenFileStyle uMode, int lpHandler);

		[DllImport("avifil32.dll", SetLastError = true)]
		public static extern void AVIFileInit();

		// Create a new stream in an existing file and creates an interface to the new stream
		[DllImport("avifil32.dll")]
		public static extern int AVIFileCreateStreamW(
			IntPtr pfile,
			out IntPtr ppavi,
			ref AVISTREAMINFOW psi);

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct AVICOMPRESSOPTIONS
		{
			public int fccType;
			public int fccHandler;
			public int dwKeyFrameEvery;
			public int dwQuality;
			public int dwBytesPerSecond;
			public int dwFlags;
			public int lpFormat;
			public int cbFormat;
			public int lpParms;
			public int cbParms;
			public int dwInterleaveEvery;
		}

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		public static extern SafeFileHandle CreateFile(
			  string lpFileName,
			  uint dwDesiredAccess,
			  uint dwShareMode,
			  IntPtr SecurityAttributes,
			  uint dwCreationDisposition,
			  uint dwFlagsAndAttributes,
			  IntPtr hTemplateFile
			  );

		[DllImport("kernel32.dll")]
		public static extern FileType GetFileType(IntPtr hFile);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		public static extern System.IntPtr GetCommandLine();

		public enum FileType : uint
		{
			FileTypeChar = 0x0002,
			FileTypeDisk = 0x0001,
			FileTypePipe = 0x0003,
			FileTypeRemote = 0x8000,
			FileTypeUnknown = 0x0000,
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr GetConsoleWindow();

		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool SetForegroundWindow(IntPtr hWnd);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr SetActiveWindow(IntPtr hWnd);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool AttachConsole(int dwProcessId);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool AllocConsole();

		[DllImport("kernel32.dll", SetLastError = false)]
		public static extern bool FreeConsole();

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr GetStdHandle(int nStdHandle);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool SetStdHandle(int nStdHandle, IntPtr hConsoleOutput);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr CreateFile(
			string fileName,
			int desiredAccess,
			int shareMode,
			IntPtr securityAttributes,
			int creationDisposition,
			int flagsAndAttributes,
			IntPtr templateFile);

		[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
		public static extern bool CloseHandle(IntPtr handle); 

		[DllImport("user32.dll", SetLastError = false)]
		public static extern IntPtr GetDesktopWindow();

		// Retrieve the save options for a file and returns them in a buffer 
		[DllImport("avifil32.dll")]
		public static extern int AVISaveOptions(
			IntPtr hwnd,
			int flags,
			int streams,
			[In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] IntPtr[] ppavi,
			[In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] IntPtr[] plpOptions);

		// Free the resources allocated by the AVISaveOptions function 
		[DllImport("avifil32.dll")]
		public static extern int AVISaveOptionsFree(
			int streams,
			[In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IntPtr[] plpOptions);

		// Create a compressed stream from an uncompressed stream and a
		// compression filter, and returns the address of a pointer to
		// the compressed stream
		[DllImport("avifil32.dll")]
		public static extern int AVIMakeCompressedStream(
			out IntPtr ppsCompressed,
			IntPtr psSource,
			ref AVICOMPRESSOPTIONS lpOptions,
			IntPtr pclsidHandler);

		// Set the format of a stream at the specified position
		[DllImport("avifil32.dll")]
		public static extern int AVIStreamSetFormat(
			IntPtr pavi,
			int lPos,
			ref BITMAPINFOHEADER lpFormat,
			int cbFormat);

		// Set the format of a stream at the specified position
		[DllImport("avifil32.dll")]
		public static extern int AVIStreamSetFormat(
			IntPtr pavi,
			int lPos,
			ref WAVEFORMATEX lpFormat,
			int cbFormat);

		// Write data to a stream
		[DllImport("avifil32.dll")]
		public static extern int AVIStreamWrite(
			IntPtr pavi,
			int lStart,
			int lSamples,
			IntPtr lpBuffer,
			int cbBuffer,
			int dwFlags,
			IntPtr plSampWritten,
			out int plBytesWritten);

		// Release an open AVI stream
		[DllImport("avifil32.dll")]
		public static extern int AVIStreamRelease(
			IntPtr pavi);

		// Release an open AVI stream
		[DllImport("avifil32.dll")]
		public static extern int AVIFileRelease(
			IntPtr pfile);


		// Replacement of mmioFOURCC macros
		public static int mmioFOURCC(string str)
		{
			return (
				((int)(byte)(str[0])) |
				((int)(byte)(str[1]) << 8) |
				((int)(byte)(str[2]) << 16) |
				((int)(byte)(str[3]) << 24));
		}

		public static bool FAILED(int hr) { return hr < 0; }



		// Inverse of mmioFOURCC
		public static string decode_mmioFOURCC(int code)
		{
			char[] chs = new char[4];

			for (int i = 0; i < 4; i++)
			{
				chs[i] = (char)(byte)((code >> (i << 3)) & 0xFF);
				if (!char.IsLetterOrDigit(chs[i]))
					chs[i] = ' ';
			}
			return new string(chs);
		}

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

		[DllImport("Kernel32.dll", EntryPoint = "RtlZeroMemory", SetLastError = false)]
		public static extern void ZeroMemory(IntPtr dest, uint size);

		[DllImport("msvcrt.dll", EntryPoint = "memset", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
		public static extern IntPtr MemSet(IntPtr dest, int c, uint count);

		[DllImport("shlwapi.dll", CharSet = CharSet.Auto)]
		public static extern bool PathRelativePathTo(
			 [Out] System.Text.StringBuilder pszPath,
			 [In] string pszFrom,
			 [In] FileAttributes dwAttrFrom,
			 [In] string pszTo,
			 [In] FileAttributes dwAttrTo
		);

		/// <summary>
		/// File attributes are metadata values stored by the file system on disk and are used by the system and are available to developers via various file I/O APIs.
		/// </summary>
		[Flags]
		[CLSCompliant(false)]
		public enum FileAttributes : uint
		{
			/// <summary>
			/// A file that is read-only. Applications can read the file, but cannot write to it or delete it. This attribute is not honored on directories. For more information, see "You cannot view or change the Read-only or the System attributes of folders in Windows Server 2003, in Windows XP, or in Windows Vista".
			/// </summary>
			Readonly = 0x00000001,

			/// <summary>
			/// The file or directory is hidden. It is not included in an ordinary directory listing.
			/// </summary>
			Hidden = 0x00000002,

			/// <summary>
			/// A file or directory that the operating system uses a part of, or uses exclusively.
			/// </summary>
			System = 0x00000004,

			/// <summary>
			/// The handle that identifies a directory.
			/// </summary>
			Directory = 0x00000010,

			/// <summary>
			/// A file or directory that is an archive file or directory. Applications typically use this attribute to mark files for backup or removal.
			/// </summary>
			Archive = 0x00000020,

			/// <summary>
			/// This value is reserved for system use.
			/// </summary>
			Device = 0x00000040,

			/// <summary>
			/// A file that does not have other attributes set. This attribute is valid only when used alone.
			/// </summary>
			Normal = 0x00000080,

			/// <summary>
			/// A file that is being used for temporary storage. File systems avoid writing data back to mass storage if sufficient cache memory is available, because typically, an application deletes a temporary file after the handle is closed. In that scenario, the system can entirely avoid writing the data. Otherwise, the data is written after the handle is closed.
			/// </summary>
			Temporary = 0x00000100,

			/// <summary>
			/// A file that is a sparse file.
			/// </summary>
			SparseFile = 0x00000200,

			/// <summary>
			/// A file or directory that has an associated reparse point, or a file that is a symbolic link.
			/// </summary>
			ReparsePoint = 0x00000400,

			/// <summary>
			/// A file or directory that is compressed. For a file, all of the data in the file is compressed. For a directory, compression is the default for newly created files and subdirectories.
			/// </summary>
			Compressed = 0x00000800,

			/// <summary>
			/// The data of a file is not available immediately. This attribute indicates that the file data is physically moved to offline storage. This attribute is used by Remote Storage, which is the hierarchical storage management software. Applications should not arbitrarily change this attribute.
			/// </summary>
			Offline = 0x00001000,

			/// <summary>
			/// The file or directory is not to be indexed by the content indexing service.
			/// </summary>
			NotContentIndexed = 0x00002000,

			/// <summary>
			/// A file or directory that is encrypted. For a file, all data streams in the file are encrypted. For a directory, encryption is the default for newly created files and subdirectories.
			/// </summary>
			Encrypted = 0x00004000,

			/// <summary>
			/// This value is reserved for system use.
			/// </summary>
			Virtual = 0x00010000
		}
	}

}