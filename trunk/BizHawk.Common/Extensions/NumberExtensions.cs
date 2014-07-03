using System.Linq;

namespace BizHawk.Common.NumberExtensions
{
	public static class NumberExtensions
	{
		public static string ToHexString(this int n, int numdigits)
		{
			return string.Format("{0:X" + numdigits + "}", n);
		}

		public static string ToHexString(this uint n, int numdigits)
		{
			return string.Format("{0:X" + numdigits + "}", n);
		}

		public static string ToHexString(this byte n, int numdigits)
		{
			return string.Format("{0:X" + numdigits + "}", n);
		}

		public static string ToHexString(this ushort n, int numdigits)
		{
			return string.Format("{0:X" + numdigits + "}", n);
		}

		public static bool Bit(this byte b, int index)
		{
			return (b & (1 << index)) != 0;
		}

		public static bool Bit(this int b, int index)
		{
			return (b & (1 << index)) != 0;
		}

		public static bool Bit(this ushort b, int index)
		{
			return (b & (1 << index)) != 0;
		}

		public static bool In(this int i, params int[] options)
		{
			return options.Any(j => i == j);
		}

		public static byte BinToBCD(this byte v)
		{
			return (byte)(((v / 10) * 16) + (v % 10));
		}

		public static byte BCDtoBin(this byte v)
		{
			return (byte)(((v / 16) * 10) + (v % 16));
		}

		/// <summary>
		/// Receives a number and returns the number of hexadecimal digits it is
		/// Note: currently only returns 2, 4, 6, or 8
		/// </summary>
		public static int NumHexDigits(this int i)
		{
			if (i < 0x100)
			{
				return 2;
			}

			if (i < 0x10000)
			{
				return 4;
			}

			if (i < 0x1000000)
			{
				return 6;
			}

			return 8;
		}
	}
}
