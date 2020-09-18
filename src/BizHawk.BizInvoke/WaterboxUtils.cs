using System;
using System.IO;
using System.Security.Cryptography;

namespace BizHawk.BizInvoke
{
	public static class WaterboxUtils
	{
		/// <summary>
		/// copy `len` bytes from `src` to `dest`
		/// </summary>
		public static void CopySome(Stream src, Stream dst, long len)
		{
			var buff = new byte[65536];
			while (len > 0)
			{
				int r = src.Read(buff, 0, (int)Math.Min(len, 65536));
				if (r == 0)
					throw new InvalidOperationException($"End of source stream was reached with {len} bytes left to copy!");
				dst.Write(buff, 0, r);
				len -= r;
			}
		}

		public static byte[] Hash(byte[] data)
		{
			using var h = SHA1.Create();
			return h.ComputeHash(data);
		}

		public static byte[] Hash(Stream s)
		{
			using var h = SHA1.Create();
			return h.ComputeHash(s);
		}

		public static unsafe void ZeroMemory(IntPtr mem, long length)
		{
			byte* p = (byte*)mem;
			byte* end = p + length;
			while (p < end)
			{
				*p++ = 0;
			}
		}

		public static long Timestamp()
		{
			return DateTime.UtcNow.Ticks;
		}

		/// <summary>
		/// system page size
		/// </summary>
		public const int PageSize = 4096;

		/// <summary>
		/// bitshift corresponding to PageSize
		/// </summary>
		public const int PageShift = 12;
		/// <summary>
		/// bitmask corresponding to PageSize
		/// </summary>
		public const ulong PageMask = 4095;

		static WaterboxUtils()
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
		{
			return (addr & PageMask) == 0;
		}

		/// <summary>
		/// align address down to previous page boundary
		/// </summary>
		public static ulong AlignDown(ulong addr)
		{
			return addr & ~PageMask;
		}

		/// <summary>
		/// align address up to next page boundary
		/// </summary>
		public static ulong AlignUp(ulong addr)
		{
			return ((addr - 1) | PageMask) + 1;
		}

		/// <summary>
		/// return the minimum number of pages needed to hold size
		/// </summary>
		public static int PagesNeeded(ulong size)
		{
			return (int)((size + PageMask) >> PageShift);
		}
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
