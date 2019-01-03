using System;
using System.Globalization;
using System.Security;

namespace BizHawk.Emulation.Common.WorkingTypes
{
	//
	// Summary:
	//		Represents an 16-bit unsigned integer, that is capable of arithmetic without making you weep.
	//		Also provides all the base functionality of the standard C# Int16 by calling its methods where relevant.
	public unsafe class wshort : IComparable, IFormattable, IComparable<wshort>, IEquatable<wshort>
	{
		private Int16 val;
		public const Int16 MaxValue = Int16.MaxValue;
		public const Int16 MinValue = Int16.MinValue;
		public static implicit operator wshort(long value)
		{
			return new wshort(value);
		}
		public static implicit operator Int16(wshort value)
		{
			return value.val;
		}
		public wshort()
		{

		}
		public wshort(long value)
		{
			val = (Int16)(value & 0xFFFF);
		}
		public wshort(ulong value)
		{
			val = (Int16)(value & 0xFFFF);
		}
		public wshort(double value)
		{
			val = (Int16)(((ulong)value) & 0xFFFF);
		}
		public static wshort Parse(string s, NumberStyles style, IFormatProvider provider)
		{
			return (long)Int16.Parse(s, style, provider);
		}
		public static wshort Parse(string s, IFormatProvider provider)
		{
			return (long)Int16.Parse(s, provider);
		}
		public static wshort Parse(string s)
		{
			return (long)Int16.Parse(s);
		}
		public static wshort Parse(string s, NumberStyles style)
		{
			return (long)Int16.Parse(s, style);
		}
		public static bool TryParse(string s, out wshort result)
		{
			result = new wshort();
			return Int16.TryParse(s, out result.val);
		}
		public static bool TryParse(string s, NumberStyles style, IFormatProvider provider, out wshort result)
		{
			result = new wshort();
			return Int16.TryParse(s, style, provider, out result.val);
		}
		public int CompareTo(wshort value)
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
		public bool Equals(wshort obj)
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