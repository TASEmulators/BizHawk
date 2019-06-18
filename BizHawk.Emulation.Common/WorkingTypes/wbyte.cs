using System;
using System.Globalization;
using System.Security;


namespace BizHawk.Emulation.Common.WorkingTypes
{
	//
	// Summary:
	//		Represents an 8-bit unsigned integer, that is capable of arithmetic without making you weep.
	//		Also provides all the base functionality of the standard C# Byte by calling its methods where relevant.
	public unsafe class wbyte : IComparable, IFormattable, IComparable<wbyte>, IEquatable<wbyte>
	{
		private Byte val;
		public const Byte MaxValue = Byte.MaxValue;
		public const Byte MinValue = Byte.MinValue;
		public static implicit operator wbyte(ulong value)
		{
			return new wbyte(value);
		}
		public static implicit operator wbyte(wushort value)
		{
			return new wbyte(value);
		}
		public static implicit operator byte(wbyte value)
		{
			return value.val;
		}
		public wbyte()
		{

		}
		public wbyte(ulong value)
		{
			val = (Byte)(value & 0xFF);
		}
		public wbyte(long value)
		{
			val = (Byte)(value & 0xFF);
		}
		public wbyte(double value)
		{
			val = (Byte)(((long)value) & 0xFF);
		}
		public static wbyte Parse(string s, NumberStyles style, IFormatProvider provider)
		{
			return (ulong)Byte.Parse(s, style, provider);
		}
		public static wbyte Parse(string s, IFormatProvider provider)
		{
			return (ulong)Byte.Parse(s, provider);
		}
		public static wbyte Parse(string s)
		{
			return (ulong)Byte.Parse(s);
		}
		public static wbyte Parse(string s, NumberStyles style)
		{
			return (ulong)Byte.Parse(s, style);
		}
		public static bool TryParse(string s, out wbyte result)
		{
			result = new wbyte();
			return byte.TryParse(s, out result.val);
		}
		public static bool TryParse(string s, NumberStyles style, IFormatProvider provider, out wbyte result)
		{
			result = new wbyte();
			return byte.TryParse(s, style, provider, out result.val);
		}
		public int CompareTo(wbyte value)
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
		public bool Equals(wbyte obj)
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