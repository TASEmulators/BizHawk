using System.Runtime.InteropServices;

#pragma warning disable IDE1006

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace BizHawk.Emulation.DiscSystem
{
	/// <summary>
	/// Bindings matching libchdr's chd.h
	/// In practice, we use chd-rs, whose c api matches libchdr's
	/// TODO: should this be common-ized? chd isn't limited to discs, it could be used for hard disk images (e.g. for MAME)
	/// </summary>
	public static class LibChd
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

		// extracted chd header (not the same as the one on disk, but rather an interpreted one by chd-rs)
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

		[DllImport("chd_capi")]
		public static extern chd_error chd_open(IntPtr filename, int mode, IntPtr parent, out IntPtr chd);

		[DllImport("chd_capi")]
		public static extern void chd_close(IntPtr chd);

		[DllImport("chd_capi")]
		public static extern chd_error chd_read(IntPtr chd, uint hunknum, byte[] buffer);

		[DllImport("chd_capi")]
		public static extern chd_error chd_read_header(IntPtr filename, ref chd_header header);

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

		/// <summary>
		/// These sub types seem to be intended to match up with how cdrdao defines .toc sub types
		/// </summary>
		public enum chd_sub_type : uint
		{
			CD_SUB_NORMAL = 0, // raw deinterleaved R-W subcode with generated P-Q subcode, 96 bytes per sector
			CD_SUB_RAW,        // raw interleaved P-W subcode, 96 bytes per sector
			CD_SUB_NONE        // no subcode data stored
		}

		// hunks should be a multiple of this for cd chds
		public const uint CD_FRAME_SIZE = 2352 + 96;

		[DllImport("chd_capi")]
		public static extern chd_error chd_get_metadata(
			IntPtr chd, uint searchtag, uint searchindex, byte[] output, uint outputlen, out uint resultlen, out uint resulttag, out byte resultflags);
	}
}
