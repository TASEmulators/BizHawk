using System.Diagnostics;

namespace BizHawk.Common
{
	// I think this is a little faster with uint than with byte
	public readonly struct Bit
	{
		private readonly uint _val;

		public Bit(uint val)
		{
			_val = val;
		}
		
		public static implicit operator Bit(int rhs)
		{
			Debug.Assert((rhs & ~1) is 0, "higher bits can't be used");
			return new Bit((uint)rhs);
		}

		public static implicit operator Bit(uint rhs)
		{
			Debug.Assert((rhs & ~1) is 0, "higher bits can't be used");
			return new Bit(rhs);
		}

		public static implicit operator Bit(byte rhs)
		{
			Debug.Assert((rhs & ~1) is 0, "higher bits can't be used");
			return new Bit(rhs);
		}

		public static implicit operator Bit(bool rhs)
		{
			return new Bit(rhs ? (byte)1 : (byte)0);
		}

		public static implicit operator long(Bit rhs)
		{
			return rhs._val;
		}

		public static implicit operator int(Bit rhs)
		{
			return (int)rhs._val;
		}

		public static implicit operator uint(Bit rhs)
		{
			return rhs._val;
		}

		public static implicit operator byte(Bit rhs)
		{
			return (byte)rhs._val;
		}

		public static implicit operator bool(Bit rhs)
		{
			return rhs._val != 0;
		}

		public override string ToString()
		{
			return _val.ToString();
		}

		public static bool operator ==(Bit lhs, Bit rhs)
		{
			return lhs._val == rhs._val;
		}

		public static bool operator !=(Bit lhs, Bit rhs)
		{
			return lhs._val != rhs._val;
		}

		public override int GetHashCode()
		{
			return _val.GetHashCode();
		}

		public override bool Equals(object? obj)
		{
			return obj is Bit b && this == b;
		}
	}
}
