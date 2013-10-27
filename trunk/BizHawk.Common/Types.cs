using System;
using System.Diagnostics;

namespace BizHawk.Common
{

	//I think this is a little faster with uint than with byte
	public struct Bit
	{
		Bit(uint val) { this.val = val; }
		uint val;
		public static implicit operator Bit(int rhs) { Debug.Assert((rhs & ~1) == 0); return new Bit((uint)(rhs)); }
		public static implicit operator Bit(uint rhs) { Debug.Assert((rhs & ~1) == 0); return new Bit((uint)(rhs)); }
		public static implicit operator Bit(byte rhs) { Debug.Assert((rhs & ~1) == 0); return new Bit((uint)(rhs)); }
		public static implicit operator Bit(bool rhs) { return new Bit(rhs ? (byte)1 : (byte)0); }
		public static implicit operator long(Bit rhs) { return (long)rhs.val; }
		public static implicit operator int(Bit rhs) { return (int)rhs.val; }
		public static implicit operator uint(Bit rhs) { return (uint)rhs.val; }
		public static implicit operator byte(Bit rhs) { return (byte)rhs.val; }
		public static implicit operator bool(Bit rhs) { return rhs.val != 0; }
		public override string ToString()
		{
			return val.ToString();
		}
		public static bool operator ==(Bit lhs, Bit rhs) { return lhs.val == rhs.val; }
		public static bool operator !=(Bit lhs, Bit rhs) { return lhs.val != rhs.val; }
		public override int GetHashCode() { return val.GetHashCode(); }
		public override bool Equals(object obj) { return this == (Bit)obj; } //this is probably wrong
	}

}