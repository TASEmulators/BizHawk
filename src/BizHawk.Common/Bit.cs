using System.Diagnostics;

namespace BizHawk.Common
{
	// I think this is a little faster with uint than with byte
	public readonly record struct Bit(uint _val)
	{
		public static implicit operator Bit(int rhs)
		{
			Debug.Assert((rhs & ~1) is 0, "higher bits can't be used");
			return new Bit((uint)rhs);
		}

		public static implicit operator Bit(bool rhs)
		{
			return new Bit(rhs ? (byte)1 : (byte)0);
		}

		public static implicit operator int(Bit rhs)
		{
			return (int)rhs._val;
		}

		public static implicit operator bool(Bit rhs)
		{
			return rhs._val != 0;
		}

		public override string ToString()
		{
			return _val.ToString();
		}
	}
}
