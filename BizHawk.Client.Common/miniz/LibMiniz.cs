using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Client.Common.Miniz
{
	public static class LibMiniz
	{
		private const string DllName = "libminiz.dll";
		private const CallingConvention CC = CallingConvention.Cdecl;

		public enum mz_zip_flags : uint
		{ 
			MZ_ZIP_FLAG_CASE_SENSITIVE = 0x0100,
			MZ_ZIP_FLAG_IGNORE_PATH = 0x0200,
			MZ_ZIP_FLAG_COMPRESSED_DATA = 0x0400,
			MZ_ZIP_FLAG_DO_NOT_SORT_CENTRAL_DIRECTORY = 0x0800
		};

		enum mz_compression_level : uint
		{
			MZ_NO_COMPRESSION = 0,
			MZ_BEST_SPEED = 1,
			MZ_BEST_COMPRESSION = 9,
			MZ_UBER_COMPRESSION = 10,
			MZ_DEFAULT_LEVEL = 6,
			MZ_DEFAULT_COMPRESSION = unchecked((uint)-1)
		};

		[DllImport(DllName, CallingConvention = CC)]
		public static extern bool mz_zip_writer_init_file(IntPtr pZip, string pFilename, long size_to_reserve_at_beginning);

		// Adds the contents of a memory buffer to an archive. These functions record the current local time into the archive.
		// To add a directory entry, call this method with an archive name ending in a forwardslash with empty buffer.
		// level_and_flags - compression level (0-10, see MZ_BEST_SPEED, MZ_BEST_COMPRESSION, etc.) logically OR'd with zero or more mz_zip_flags, or just set to MZ_DEFAULT_COMPRESSION.
		[DllImport(DllName, CallingConvention = CC)]
		public static extern bool mz_zip_writer_add_mem(IntPtr pZip, string pArchive_name, byte[] pBuf, ulong buf_size, uint level_and_flags);

		// Finalizes the archive by writing the central directory records followed by the end of central directory record.
		// After an archive is finalized, the only valid call on the mz_zip_archive struct is mz_zip_writer_end().
		// An archive must be manually finalized by calling this function for it to be valid.
		[DllImport(DllName, CallingConvention = CC)]
		public static extern bool mz_zip_writer_finalize_archive(IntPtr pZip);

		// Ends archive writing, freeing all allocations, and closing the output file if mz_zip_writer_init_file() was used.
		// Note for the archive to be valid, it must have been finalized before ending.
		[DllImport(DllName, CallingConvention = CC)]
		public static extern bool mz_zip_writer_end(IntPtr pZip);
	}
}
