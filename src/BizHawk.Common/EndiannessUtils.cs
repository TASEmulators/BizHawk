using System;
using System.Diagnostics;

namespace BizHawk.Common
{
	public static unsafe class EndiannessUtils
	{
		/// <summary>reverses pairs of octets: <c>0xAABBIIJJPPQQYYZZ</c> &lt;=> <c>0xBBAAJJIIQQPPZZYY</c></summary>
		public static byte[] ByteSwap16(byte[] a)
		{
			var l = a.Length;
			var copy = new byte[l];
			Array.Copy(a, copy, l);
			MutatingByteSwap16(copy);
			return copy;
		}

		/// <summary>reverses groups of 4 octets: <c>0xAABBCCDDWWXXYYZZ</c> &lt;=> <c>0xDDCCBBAAZZYYXXWW</c></summary>
		public static byte[] ByteSwap32(byte[] a)
		{
			var l = a.Length;
			var copy = new byte[l];
			Array.Copy(a, copy, l);
			MutatingByteSwap32(copy);
			return copy;
		}

		/// <summary>reverses pairs of octets in-place: <c>0xAABBIIJJPPQQYYZZ</c> &lt;=> <c>0xBBAAJJIIQQPPZZYY</c></summary>
		public static void MutatingByteSwap16(byte[] a)
		{
			var l = a.Length;
			Debug.Assert(l % 2 == 0);
			fixed (byte* p = &a[0]) for (var i = 0; i < l; i += 2)
			{
				var b = p[i];
				p[i] = p[i + 1];
				p[i + 1] = b;
			}
		}

		/// <summary>reverses groups of 4 octets in-place: <c>0xAABBCCDDWWXXYYZZ</c> &lt;=> <c>0xDDCCBBAAZZYYXXWW</c></summary>
		public static void MutatingByteSwap32(byte[] a)
		{
			var l = a.Length;
			Debug.Assert(l % 4 == 0);
			fixed (byte* p = &a[0]) for (var i = 0; i < l; i += 4)
			{
				var b = p[i];
				p[i] = p[i + 3];
				p[i + 3] = b;
				b = p[i + 1];
				p[i + 1] = p[i + 2];
				p[i + 2] = b;
			}
		}
	}
}
