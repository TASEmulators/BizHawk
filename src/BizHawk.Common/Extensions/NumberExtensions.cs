using System.Linq;
using System.Runtime.CompilerServices;

namespace BizHawk.Common.NumberExtensions
{
	public static class NumberExtensions
	{
		private const string ERR_MSG_PRECISION_LOSS = "unable to convert from decimal without loss of precision";

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

		/// <returns>the <see langword="float"/> whose value is closest to <paramref name="m"/></returns>
		/// <exception cref="OverflowException">loss of precision (the value won't survive a round-trip)</exception>
		/// <remarks>like a <c>checked</c> conversion</remarks>
		public static float ConvertToF32(this decimal m)
		{
			var f = decimal.ToSingle(m);
			return m.Equals(new decimal(f)) ? f : throw new OverflowException(ERR_MSG_PRECISION_LOSS);
		}

		/// <returns>the <see langword="double"/> whose value is closest to <paramref name="m"/></returns>
		/// <exception cref="OverflowException">loss of precision (the value won't survive a round-trip)</exception>
		/// <remarks>like a <c>checked</c> conversion</remarks>
		public static double ConvertToF64(this decimal m)
		{
			var d = decimal.ToDouble(m);
			return m.Equals(new decimal(d)) ? d : throw new OverflowException(ERR_MSG_PRECISION_LOSS);
		}

		/// <returns>the <see langword="decimal"/> whose value is closest to <paramref name="f"/></returns>
		/// <exception cref="NotFiniteNumberException">
		/// iff <paramref name="f"/> is NaN and <paramref name="throwIfNaN"/> is set
		/// (infinite values are rounded to <see cref="decimal.MinValue"/>/<see cref="decimal.MaxValue"/>)
		/// </exception>
		/// <remarks>like an <c>unchecked</c> conversion</remarks>
		public static decimal ConvertToMoneyTruncated(float f, bool throwIfNaN = false)
		{
			try
			{
#pragma warning disable BHI1105 // this is the sanctioned call-site
				return (decimal) f;
#pragma warning restore BHI1105
			}
			catch (OverflowException)
			{
				return float.IsNaN(f)
					? throwIfNaN
						? throw new NotFiniteNumberException(f)
						: default
					: f < 0.0f
						? decimal.MinValue
						: decimal.MaxValue;
			}
		}

		/// <returns>the <see langword="decimal"/> whose value is closest to <paramref name="d"/></returns>
		/// <exception cref="NotFiniteNumberException">
		/// iff <paramref name="d"/> is NaN and <paramref name="throwIfNaN"/> is set
		/// (infinite values are rounded to <see cref="decimal.MinValue"/>/<see cref="decimal.MaxValue"/>)
		/// </exception>
		/// <remarks>like an <c>unchecked</c> conversion</remarks>
		public static decimal ConvertToMoneyTruncated(double d, bool throwIfNaN = false)
		{
			try
			{
#pragma warning disable BHI1105 // this is the sanctioned call-site
				return (decimal) d;
#pragma warning restore BHI1105
			}
			catch (OverflowException)
			{
				return double.IsNaN(d)
					? throwIfNaN
						? throw new NotFiniteNumberException(d)
						: default
					: d < 0.0
						? decimal.MinValue
						: decimal.MaxValue;
			}
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

			if (i < 0x1_0000)
			{
				return 4;
			}

			if (i < 0x100_0000)
			{
				return 6;
			}

			if (i < 0x1_0000_0000)
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IntPtr Plus(this IntPtr p, int offset)
			=> p + offset;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IntPtr Plus(this IntPtr p, uint offset)
		{
			var half = unchecked((int) offset >> 1);
			return p + half + unchecked((int) (offset - half));
		}

		public static void RotateRightU8(ref byte b, int shift)
		{
			byte temp = b;
			temp <<= 8 - shift;
			b >>= shift;
			b |= temp;
		}

		public static int RoundToInt(this double d) => (int) Math.Round(d);

		public static int RoundToInt(this float f) => (int) Math.Round(f);

		/// <summary>2^-53</summary>
		private const double ExtremelySmallNumber = 1.1102230246251565E-16;

		/// <inheritdoc cref="HawkFloatEquality(float,float,float)"/>
		public static bool HawkFloatEquality(this double d, double other, double ε = ExtremelySmallNumber) => Math.Abs(other - d) < ε;

		/// <summary>2^-24</summary>
		private const float ReallySmallNumber = 5.96046448E-08f;

		/// <remarks>don't use this in cores without picking a suitable ε</remarks>
		public static bool HawkFloatEquality(this float f, float other, float ε = ReallySmallNumber) => Math.Abs(other - f) < ε;

		/// <summary> Reinterprets the byte representation of <paramref name="value"/> as a float</summary>
		public static float ReinterpretAsF32(uint value) => Unsafe.As<uint, float>(ref value);

		/// <summary> Reinterprets the byte representation of <paramref name="value"/> as a uint</summary>
		public static uint ReinterpretAsUInt32(float value) => Unsafe.As<float, uint>(ref value);
	}
}
