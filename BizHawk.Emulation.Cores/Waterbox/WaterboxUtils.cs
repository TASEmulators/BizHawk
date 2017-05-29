using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace BizHawk.Emulation.Cores.Waterbox
{
	public static class WaterboxUtils
	{
		/// <summary>
		/// copy `len` bytes from `src` to `dest`
		/// </summary>
		/// <param name="src"></param>
		/// <param name="dst"></param>
		/// <param name="len"></param>
		public static void CopySome(Stream src, Stream dst, long len)
		{
			var buff = new byte[4096];
			while (len > 0)
			{
				int r = src.Read(buff, 0, (int)Math.Min(len, 4096));
				dst.Write(buff, 0, r);
				len -= r;
			}
		}

		public static byte[] Hash(byte[] data)
		{
			using (var h = SHA1.Create())
			{
				return h.ComputeHash(data);
			}
		}

		public static byte[] Hash(Stream s)
		{
			using (var h = SHA1.Create())
			{
				return h.ComputeHash(s);
			}
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
		public static int PageSize { get; private set; }

		/// <summary>
		/// bitshift corresponding to PageSize
		/// </summary>
		public static int PageShift { get; private set; }
		/// <summary>
		/// bitmask corresponding to PageSize
		/// </summary>
		public static ulong PageMask { get; private set; }

		static WaterboxUtils()
		{
			int p = PageSize = Environment.SystemPageSize;
			while (p != 1)
			{
				p >>= 1;
				PageShift++;
			}
			PageMask = (ulong)(PageSize - 1);
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
