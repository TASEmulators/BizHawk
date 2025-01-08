using System.Diagnostics;

namespace BizHawk.Common
{
	public static unsafe class EndiannessUtils
	{
		/// <summary>reverses pairs of octets in-place: <c>0xAABBIIJJPPQQYYZZ</c> &lt;=> <c>0xBBAAJJIIQQPPZZYY</c></summary>
		public static void MutatingByteSwap16(Span<byte> a)
		{
#if true //TODO benchmark (both methods work correctly); also there is another method involving shifting the (output copy of the) array over by 1 byte then manually writing every second byte
			var l = a.Length;
			Debug.Assert(l % 2 is 0, "octets must be in pairs");
			fixed (byte* p = &a[0]) for (var i = 0; i < l; i += 2)
			{
				var b = p[i];
				p[i] = p[i + 1];
				p[i + 1] = b;
			}
#else
			Debug.Assert(a.Length % 2 is 0, "octets must be in pairs");
			var shorts = MemoryMarshal.Cast<byte, ushort>(a);
			for (var i = 0; i < shorts.Length; i++) shorts[i] = BinaryPrimitives.ReverseEndianness(shorts[i]);
#endif
		}

		/// <summary>reverses groups of 4 octets in-place: <c>0xAABBCCDDWWXXYYZZ</c> &lt;=> <c>0xDDCCBBAAZZYYXXWW</c></summary>
		public static void MutatingByteSwap32(Span<byte> a)
		{
#if true //TODO benchmark (both methods work correctly)
			var l = a.Length;
			Debug.Assert(l % 4 is 0, "octets must be in groups of 4");
			fixed (byte* p = &a[0]) for (var i = 0; i < l; i += 4)
			{
				var b = p[i];
				p[i] = p[i + 3];
				p[i + 3] = b;
				b = p[i + 1];
				p[i + 1] = p[i + 2];
				p[i + 2] = b;
			}
#else
			Debug.Assert(a.Length % 4 is 0, "octets must be in groups of 4");
			var ints = MemoryMarshal.Cast<byte, uint>(a);
			for (var i = 0; i < ints.Length; i++) ints[i] = BinaryPrimitives.ReverseEndianness(ints[i]);
#endif
		}

		/// <summary>swaps pairs of 16-bit words in-place: <c>0xAABBIIJJPPQQYYZZ</c> &lt;=> <c>0xIIJJAABBYYZZPPQQ</c></summary>
		public static void MutatingShortSwap32(Span<byte> a)
		{
			// no need to optimise this further, it's only used in the manual byteswap tool and only between the two less common formats
			MutatingByteSwap32(a); // calls can be in either order, though this order ensures the length-mod-4 assert is hit immediately
			MutatingByteSwap16(a);
		}
	}
}
