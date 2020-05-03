#nullable disable

using System;
using System.Linq;

namespace BizHawk.Common.NumberExtensions
{
	public static class NumberExtensions
	{
		public static string ToHexString(this int n, int numDigits)
		{
			return string.Format($"{{0:X{numDigits}}}", n);
		}

		public static string ToHexString(this uint n, int numDigits)
		{
			return string.Format($"{{0:X{numDigits}}}", n);
		}

		public static string ToHexString(this long n, int numDigits)
		{
			return string.Format($"{{0:X{numDigits}}}", n);
		}

		public static string ToHexString(this ulong n, int numDigits)
		{
			return string.Format($"{{0:X{numDigits}}}", n);
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
		public static int NumHexDigits(this long i)
		{
			// now this is a bit of a trick. if it was less than 0, it must have been >= 0x80000000 and so takes all 8 digits
			if (i < 0)
			{
				return 8;
			}

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

			if (i < 0x100000000)
			{
				return 8;
			}

			return 16;
		}

		/// <summary>
		/// The % operator is a remainder operator. (e.g. -1 mod 4 returns -1, not 3.)
		/// </summary>
		public static int Mod(this int a, int b)
		{
			return a - (b * (int)Math.Floor((float)a / b));
		}

		/// <summary>
		/// Force the value to be strictly between min and max (both excluded)
		/// </summary>
		/// <typeparam name="T">Anything that implements <see cref="IComparable{T}"/></typeparam>
		/// <param name="val">Value that will be clamped</param>
		/// <param name="min">Minimum allowed</param>
		/// <param name="max">Maximum allowed</param>
		/// <returns>The value if strictly between min and max; otherwise min (or max depending of what is passed)</returns>
		public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
		{
			if (val.CompareTo(min) < 0)
			{
				return min;
			}

			if (val.CompareTo(max) > 0)
			{
				return max;
			}

			return val;
		}
		
		public static int RoundToInt(this float f) => (int) Math.Round(f);

		/// <summary>2^-53</summary>
		private const double ExtremelySmallNumber = 1.1102230246251565E-16;

		/// <inheritdoc cref="HawkFloatEquality(float,float,float)"/>
		public static bool HawkFloatEquality(this double d, double other, double ε = ExtremelySmallNumber) => Math.Abs(other - d) < ε;

		/// <summary>2^-24</summary>
		private const float ReallySmallNumber = 5.96046448E-08f;

		/// <remarks>don't use this in cores without picking a suitable ε</remarks>
		public static bool HawkFloatEquality(this float f, float other, float ε = ReallySmallNumber) => Math.Abs(other - f) < ε;
	}
}
