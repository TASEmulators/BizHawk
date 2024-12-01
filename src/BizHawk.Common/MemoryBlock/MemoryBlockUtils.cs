using System.Buffers;
using System.IO;

namespace BizHawk.Common
{
	public static class MemoryBlockUtils
	{
		/// <summary>
		/// copy `len` bytes from `src` to `dest`
		/// </summary>
		public static void CopySome(Stream src, Stream dst, long len)
		{
			const int TEMP_BUFFER_LENGTH = 65536;
			var tmpBuf = ArrayPool<byte>.Shared.Rent(TEMP_BUFFER_LENGTH);
			try
			{
				while (len > 0)
				{
					var r = src.Read(tmpBuf, 0, (int)Math.Min(len, TEMP_BUFFER_LENGTH));
					if (r == 0)
					{
						throw new InvalidOperationException($"End of source stream was reached with {len} bytes left to copy!");
					}

					dst.Write(tmpBuf, 0, r);
					len -= r;
				}
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(tmpBuf);
			}
		}

		/// <summary>
		/// System page size, currently hardcoded/assumed to be 4096
		/// </summary>
		public const int PageSize = 1 << PageShift;

		/// <summary>
		/// bitshift corresponding to PageSize
		/// </summary>
		public const int PageShift = 12;

		/// <summary>
		/// bitmask corresponding to PageSize
		/// </summary>
		public const ulong PageMask = PageSize - 1;

		static MemoryBlockUtils()
		{
			if (PageSize != Environment.SystemPageSize)
			{
				// We can do it, but we'll have to change up some waterbox stuff
				throw new InvalidOperationException("Wrong page size");
			}
		}

		/// <summary>
		/// true if addr is aligned
		/// </summary>
		public static bool Aligned(ulong addr)
			=> (addr & PageMask) == 0;

		/// <summary>
		/// align address down to previous page boundary
		/// </summary>
		public static ulong AlignDown(ulong addr)
			=> addr & ~PageMask;

		/// <summary>
		/// align address up to next page boundary
		/// </summary>
		public static ulong AlignUp(ulong addr)
			=> ((addr - 1) | PageMask) + 1;

		/// <summary>
		/// return the minimum number of pages needed to hold size
		/// </summary>
		public static int PagesNeeded(ulong size)
			=> (int)((size + PageMask) >> PageShift);
	}

	// C# is annoying:  arithmetic operators for native ints are not exposed.
	// So we store them as long/ulong instead in many places, and use these helpers
	// to convert to IntPtr when needed

	public static class Z
	{
		public static IntPtr US(ulong l)
		{
			if (IntPtr.Size == 8)
				return (IntPtr)(long)l;
			else
				return (IntPtr)(int)l;
		}

		public static UIntPtr UU(ulong l)
		{
			if (UIntPtr.Size == 8)
				return (UIntPtr)l;
			else
				return (UIntPtr)(uint)l;
		}

		public static IntPtr SS(long l)
		{
			if (IntPtr.Size == 8)
				return (IntPtr)l;
			else
				return (IntPtr)(int)l;
		}

		public static UIntPtr SU(long l)
		{
			if (UIntPtr.Size == 8)
				return (UIntPtr)(ulong)l;
			else
				return (UIntPtr)(uint)l;
		}
	}
}
