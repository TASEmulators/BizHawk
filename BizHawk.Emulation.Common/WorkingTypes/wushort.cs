using System;
using System.Globalization;
using System.Security;


namespace BizHawk.Emulation.Common.WorkingTypes
{
	//
	// Summary:
	//		Represents an 16-bit unsigned integer, that is capable of arithmetic without making you weep.
	//		Also provides all the base functionality of the standard C# UInt16 by calling its methods where relevant.
	public unsafe class wushort : IComparable, IFormattable, IComparable<wushort>, IEquatable<wushort>
	{
		private UInt16 val;
		public const UInt16 MaxValue = UInt16.MaxValue;
		public const UInt16 MinValue = UInt16.MinValue;
		public static implicit operator wushort(ulong value)
		{
			return new wushort(value);
		}
		public static implicit operator wushort(wbyte value)
		{
			return new wushort(value);
		}
		public static implicit operator UInt16(wushort value)
		{
			return value.val;
		}
		public wushort()
		{

		}
		public wushort(ulong value)
		{
			val = (UInt16)(value & 0xFFFF);
		}
		public wushort(long value)
		{
			val = (UInt16)(value & 0xFFFF);
		}
		public wushort(double value)
		{
			val = (UInt16)(((long)value) & 0xFFFF);
		}
		public static wushort Parse(string s, NumberStyles style, IFormatProvider provider)
		{
			return (uint)UInt16.Parse(s, style, provider);
		}
		public static wushort Parse(string s, IFormatProvider provider)
		{
			return (uint)UInt16.Parse(s, provider);
		}
		public static wushort Parse(string s)
		{
			return (uint)UInt16.Parse(s);
		}
		public static wushort Parse(string s, NumberStyles style)
		{
			return (uint)UInt16.Parse(s, style);
		}
		public static bool TryParse(string s, out wushort result)
		{
			result = new wushort();
			return ushort.TryParse(s, out result.val);
		}
		public static bool TryParse(string s, NumberStyles style, IFormatProvider provider, out wushort result)
		{
			result = new wushort();
			return ushort.TryParse(s, style, provider, out result.val);
		}
		public int CompareTo(wushort value)
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
		public bool Equals(wushort obj)
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