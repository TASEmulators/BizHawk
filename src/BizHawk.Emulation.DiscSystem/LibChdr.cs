using System;
using System.IO;
using System.Runtime.InteropServices;

#pragma warning disable IDE1006

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace BizHawk.Emulation.DiscSystem
{
	/// <summary>
	/// libchdr bindings
	/// TODO: should this be common-ized? chd isn't limited to discs, it could be used for hard disk images (e.g. for MAME)
	/// </summary>
	public static class LibChdr
	{
		public const uint CHD_HEADER_VERSION = 5;
		public const uint CHD_V5_HEADER_SIZE = 124;

		public const int CHD_MD5_BYTES = 16;
		public const int CHD_SHA1_BYTES = 20;

		public const byte CHD_MDFLAGS_CHECKSUM = 0x01;

		public const uint CHD_CODEC_ZSTD = 0x7A737464; // zstd
		public const uint CHD_CODEC_CD_ZSTD = 0x63647A73; // cdzs

		public const uint CDROM_OLD_METADATA_TAG = 0x43484344; // CHCD
		public const uint CDROM_TRACK_METADATA_TAG = 0x43485452; // CHTR
		public const uint CDROM_TRACK_METADATA2_TAG = 0x43485432; // CHT2

		// these formats are more for sscanf, they aren't suitable for C#
		public const string CDROM_TRACK_METADATA_FORMAT = "TRACK:%d TYPE:%s SUBTYPE:%s FRAMES:%d";
		public const string CDROM_TRACK_METADATA2_FORMAT = "TRACK:%d TYPE:%s SUBTYPE:%s FRAMES:%d PREGAP:%d PGTYPE:%s PGSUB:%s POSTGAP:%d";

		public const int CHD_OPEN_READ = 1;
		public const int CHD_OPEN_READWRITE = 2;

		public enum chd_error : int
		{
			CHDERR_NONE,
			CHDERR_NO_INTERFACE,
			CHDERR_OUT_OF_MEMORY,
			CHDERR_INVALID_FILE,
			CHDERR_INVALID_PARAMETER,
			CHDERR_INVALID_DATA,
			CHDERR_FILE_NOT_FOUND,
			CHDERR_REQUIRES_PARENT,
			CHDERR_FILE_NOT_WRITEABLE,
			CHDERR_READ_ERROR,
			CHDERR_WRITE_ERROR,
			CHDERR_CODEC_ERROR,
			CHDERR_INVALID_PARENT,
			CHDERR_HUNK_OUT_OF_RANGE,
			CHDERR_DECOMPRESSION_ERROR,
			CHDERR_COMPRESSION_ERROR,
			CHDERR_CANT_CREATE_FILE,
			CHDERR_CANT_VERIFY,
			CHDERR_NOT_SUPPORTED,
			CHDERR_METADATA_NOT_FOUND,
			CHDERR_INVALID_METADATA_SIZE,
			CHDERR_UNSUPPORTED_VERSION,
			CHDERR_VERIFY_INCOMPLETE,
			CHDERR_INVALID_METADATA,
			CHDERR_INVALID_STATE,
			CHDERR_OPERATION_PENDING,
			CHDERR_NO_ASYNC_OPERATION,
			CHDERR_UNSUPPORTED_FORMAT
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct chd_core_file
		{
			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			public delegate ulong FSizeDelegate(IntPtr file);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			public delegate nuint FReadDelegate(IntPtr buffer, nuint size, nuint count, IntPtr file);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			public delegate int FCloseDelegate(IntPtr file);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			public delegate int FSeekDelegate(IntPtr file, long offset, SeekOrigin origin);

			public IntPtr argp;
			[MarshalAs(UnmanagedType.FunctionPtr)]
			public FSizeDelegate fsize;
			[MarshalAs(UnmanagedType.FunctionPtr)]
			public FReadDelegate fread;
			[MarshalAs(UnmanagedType.FunctionPtr)]
			public FCloseDelegate fclose;
			[MarshalAs(UnmanagedType.FunctionPtr)]
			public FSeekDelegate fseek;
		}

		/// <summary>
		/// Convenience chd_core_file wrapper against a generic Stream
		/// </summary>
		public class CoreFileStreamWrapper : IDisposable
		{
			private const uint READ_BUFFER_LEN = 8 * CD_FRAME_SIZE; // 8 frames, usual uncompressed hunk size
			private readonly byte[] _readBuffer = new byte[READ_BUFFER_LEN];

			private Stream _s;

			// ReSharper disable once MemberCanBePrivate.Global
			private readonly chd_core_file _coreFile;
			public readonly IntPtr CoreFile;

			private ulong FSize(IntPtr file)
			{
				try
				{
					return (ulong)_s.Length;
				}
				catch (Exception e)
				{
					Console.Error.WriteLine(e);
					return unchecked((ulong)-1);
				}
			}

			private nuint FRead(IntPtr buffer, nuint size, nuint count, IntPtr file)
			{
				nuint ret = 0;
				try
				{
					// note: size will always be 1, so this should never overflow
					var numBytesToRead = (uint)Math.Min(size * (ulong)count, uint.MaxValue);
					while (numBytesToRead > 0)
					{
						var numRead = _s.Read(_readBuffer, 0, (int)Math.Min(READ_BUFFER_LEN, numBytesToRead));
						if (numRead == 0)
						{
							return ret;
						}

						Marshal.Copy(_readBuffer, 0, buffer, numRead);
						buffer += numRead;
						ret += (uint)numRead;
						numBytesToRead -= (uint)numRead;
					}

					return ret;
				}
				catch (Exception e)
				{
					Console.Error.WriteLine(e);
					return ret;
				}
			}

			private int FClose(IntPtr file)
			{
				if (_s == null)
				{
					return -1;
				}

				_s.Dispose();
				_s = null;
				return 0;
			}

			private int FSeek(IntPtr file, long offset, SeekOrigin origin)
			{
				try
				{
					_s.Seek(offset, origin);
					return 0;
				}
				catch (Exception ex)
				{
					Console.Error.WriteLine(ex);
					return -1;
				}
			}

			public CoreFileStreamWrapper(Stream s)
			{
				if (!s.CanRead || !s.CanSeek)
				{
					throw new NotSupportedException("The underlying CHD stream must support reading and seeking!");
				}

				_s = s;
				_coreFile.fsize = FSize;
				_coreFile.fread = FRead;
				_coreFile.fclose = FClose;
				_coreFile.fseek = FSeek;
				// the pointer here must stay alloc'd on the unmanaged size
				// as libchdr expects the memory to not move around
				CoreFile = Marshal.AllocCoTaskMem(Marshal.SizeOf<chd_core_file>());
				Marshal.StructureToPtr(_coreFile, CoreFile, fDeleteOld: false);
			}

			public void Dispose()
			{
				Marshal.DestroyStructure<chd_core_file>(CoreFile);
				Marshal.FreeCoTaskMem(CoreFile);
				_s?.Dispose();
			}
		}

		[DllImport("chdr")]
		public static extern chd_error chd_open_core_file(IntPtr file, int mode, IntPtr parent, out IntPtr chd);

		[DllImport("chdr")]
		public static extern void chd_close(IntPtr chd);

		// extracted chd header (not the same as the one on disk, but rather an interpreted one by libchdr)
		[StructLayout(LayoutKind.Sequential)]
		public struct chd_header
		{
			public uint length;                                  // length of header data
			public uint version;                                 // drive format version
			public uint flags;                                   // flags field
			public unsafe fixed uint compression[4];             // compression type
			public uint hunkbytes;                               // number of bytes per hunk
			public uint totalhunks;                              // total # of hunks represented
			public ulong logicalbytes;                           // logical size of the data
			public ulong metaoffset;                             // offset in file of first metadata
			public ulong mapoffset;                              // TOOD V5
			public unsafe fixed byte md5[CHD_MD5_BYTES];         // overall MD5 checksum
			public unsafe fixed byte parentmd5[CHD_MD5_BYTES];   // overall MD5 checksum of parent
			public unsafe fixed byte sha1[CHD_SHA1_BYTES];       // overall SHA1 checksum
			public unsafe fixed byte rawsha1[CHD_SHA1_BYTES];    // SHA1 checksum of raw data
			public unsafe fixed byte parentsha1[CHD_SHA1_BYTES]; // overall SHA1 checksum of parent
			public uint unitbytes;                               // TODO V5
			public ulong unitcount;                              // TODO V5
			public uint hunkcount;                               // TODO V5
			public uint mapentrybytes;                           // length of each entry in a map (V5)
			public unsafe byte* rawmap;                          // raw map data
			public uint obsolete_cylinders;                      // obsolete field -- do not use!
			public uint obsolete_sectors;                        // obsolete field -- do not use!
			public uint obsolete_heads;                          // obsolete field -- do not use!
			public uint obsolete_hunksize;                       // obsolete field -- do not use!
		}

		[DllImport("chdr")]
		public static extern IntPtr chd_get_header(IntPtr chd);

		[DllImport("chdr")]
		public static extern chd_error chd_read(IntPtr chd, uint hunknum, byte[] buffer);

		public enum chd_track_type : uint
		{
			CD_TRACK_MODE1 = 0,      // mode 1 2048 bytes/sector
			CD_TRACK_MODE1_RAW,      // mode 1 2352 bytes/sector
			CD_TRACK_MODE2,          // mode 2 2336 bytes/sector
			CD_TRACK_MODE2_FORM1,    // mode 2 2048 bytes/sector
			CD_TRACK_MODE2_FORM2,    // mode 2 2324 bytes/sector
			CD_TRACK_MODE2_FORM_MIX, // mode 2 2336 bytes/sector
			CD_TRACK_MODE2_RAW,      // mode 2 2352 bytes/sector
			CD_TRACK_AUDIO,          // redbook audio track 2352 bytes/sector (588 samples)
		}

		public enum chd_sub_type : uint
		{
			CD_SUB_NORMAL = 0, // "cooked" 96 bytes per sector
			CD_SUB_RAW,        // raw uninterleaved 96 bytes per sector
			CD_SUB_NONE        // no subcode data stored
		}

		// hunks should be a multiple of this for cd chds
		public const uint CD_FRAME_SIZE = 2352 + 96;

		[DllImport("chdr")]
		public static extern chd_error chd_get_metadata(
			IntPtr chd, uint searchtag, uint searchindex, byte[] output, uint outputlen, out uint resultlen, out uint resulttag, out byte resultflags);
	}
}
