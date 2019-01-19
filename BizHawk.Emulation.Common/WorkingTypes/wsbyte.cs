using System;
using System.Globalization;
using System.Security;

namespace BizHawk.Emulation.Common.WorkingTypes
{
	//
	// Summary:
	//		Represents an 8-bit unsigned integer, that is capable of arithmetic without making you weep.
	//		Also provides all the base functionality of the standard C# SByte by calling its methods where relevant.
	public unsafe class wsbyte : IComparable, IFormattable, IComparable<wsbyte>, IEquatable<wsbyte>
	{
		private SByte val;
		public const SByte MaxValue = SByte.MaxValue;
		public const SByte MinValue = SByte.MinValue;
		public static implicit operator wsbyte(long value)
		{
			return new wsbyte(value);
		}
		public static implicit operator SByte(wsbyte value)
		{
			return value.val;
		}
		public wsbyte()
		{

		}
		public wsbyte(long value)
		{
			val = (SByte)(value & 0xFF);
		}
		public wsbyte(ulong value)
		{
			val = (SByte)(value & 0xFF);
		}
		public wsbyte(double value)
		{
			val = (SByte)(((ulong)value) & 0xFF);
		}
		public static wsbyte Parse(string s, NumberStyles style, IFormatProvider provider)
		{
			return (long)SByte.Parse(s, style, provider);
		}
		public static wsbyte Parse(string s, IFormatProvider provider)
		{
			return (long)SByte.Parse(s, provider);
		}
		public static wsbyte Parse(string s)
		{
			return (long)SByte.Parse(s);
		}
		public static wsbyte Parse(string s, NumberStyles style)
		{
			return (long)SByte.Parse(s, style);
		}
		public static bool TryParse(string s, out wsbyte result)
		{
			result = new wsbyte();
			return SByte.TryParse(s, out result.val);
		}
		public static bool TryParse(string s, NumberStyles style, IFormatProvider provider, out wsbyte result)
		{
			result = new wsbyte();
			return SByte.TryParse(s, style, provider, out result.val);
		}
		public int CompareTo(wsbyte value)
		{
			return val.CompareTo(value.val);
		}
		public int CompareTo(object value)
		{
			return val.CompareTo(value);
		}
		public override bool Equals(object obj)
		{
			return val.Equals(obj);
		}
		public bool Equals(wsbyte obj)
		{
			return val.Equals(obj);
		}
		public override int GetHashCode()
		{
			return val.GetHashCode();
		}
		public TypeCode GetTypeCode()
		{
			return val.GetTypeCode();
		}
		[SecuritySafeCritical]
		public string ToString(string format, IFormatProvider provider)
		{
			return val.ToString(format, provider);
		}
		[SecuritySafeCritical]
		public override string ToString()
		{
			return val.ToString();
		}
		[SecuritySafeCritical]
		public string ToString(string format)
		{
			return val.ToString(format);
		}
		[SecuritySafeCritical]
		public string ToString(IFormatProvider provider)
		{
			return val.ToString(provider);
		}
	}
}