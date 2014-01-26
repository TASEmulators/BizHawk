using System;

namespace BizHawk.Client.Common
{
	public class BitLuaLibrary : LuaLibraryBase
	{
		public override string Name { get { return "bit"; } }

		[LuaMethodAttributes(
			"band", 
			"Bitwise AND of 'val' against 'amt'"
		)]
		public static uint Band(object val, object amt)
		{
			return (uint)(LuaInt(val) & LuaInt(amt));
		}

		[LuaMethodAttributes(
			"bnot",
			"Bitwise NOT of 'val' against 'amt'"
		)]
		public static uint Bnot(object val)
		{
			return (uint)(~LuaInt(val));
		}

		[LuaMethodAttributes(
			"bor",
			"Bitwise OR of 'val' against 'amt'"
		)]
		public static uint Bor(object val, object amt)
		{
			return (uint)(LuaInt(val) | LuaInt(amt));
		}

		[LuaMethodAttributes(
			"bxor",
			"Bitwise XOR of 'val' against 'amt'"
		)]
		public static uint Bxor(object val, object amt)
		{
			return (uint)(LuaInt(val) ^ LuaInt(amt));
		}

		[LuaMethodAttributes(
			"lshift",
			"Logical shift left of 'val' by 'amt' bits"
		)]
		public static uint Lshift(object val, object amt)
		{
			return (uint)(LuaInt(val) << LuaInt(amt));
		}

		[LuaMethodAttributes(
			"rol",
			"Left rotate 'val' by 'amt' bits"
		)]
		public static uint Rol(object val, object amt)
		{
			return (uint)((LuaInt(val) << LuaInt(amt))
				| (LuaInt(val) >> (32 - LuaInt(amt))));
		}

		[LuaMethodAttributes(
			"ror",
			"Right rotate 'val' by 'amt' bits"
		)]
		public static uint Ror(object val, object amt)
		{
			return (uint)((LuaInt(val) >> LuaInt(amt))
				| (LuaInt(val) << (32 - LuaInt(amt))));
		}

		[LuaMethodAttributes(
			"rshift",
			"Logical shift right of 'val' by 'amt' bits"
		)]
		public static uint Rshift(object val, object amt)
		{
			return (uint)(LuaInt(val) >> LuaInt(amt));
		}

		[LuaMethodAttributes(
			"check",
			"Returns result of bit 'pos' being set in 'num'"
		)]
		public static bool Check(object num, object pos)
		{
			return (LuaLong(num) & (1 << LuaInt(pos))) != 0;
		}

		[LuaMethodAttributes(
			"set",
			"TODO"
		)]
		public static uint Set(object num, object pos)
		{
			return (uint)(LuaInt(num) | 1 << LuaInt(pos));
		}

		[LuaMethodAttributes(
			"clear",
			"TODO"
		)]
		public static uint Clear(object num, object pos)
		{
			return (uint)(LuaInt(num) & ~(1 << LuaInt(pos)));
		}

		[LuaMethodAttributes(
			"byteswap_16",
			"Byte swaps 'short', i.e. bit.byteswap_16(0xFF00) would return 0x00FF"
		)]
		public static uint Byteswap_16(object _short)
		{
			 return (UInt16)((LuaInt(_short) & 0xFFU) << 8 | (LuaInt(_short) & 0xFF00U) >> 8);
		}

		[LuaMethodAttributes(
			"byteswap_32",
			"Byte swaps 'dword'"
		)]
		public static uint Byteswap_32(object _dword)
		{
			return (LuaUInt(_dword) & 0x000000FFU) << 24 | (LuaUInt(_dword) & 0x0000FF00U) << 8 |
				(LuaUInt(_dword) & 0x00FF0000U) >> 8 | (LuaUInt(_dword) & 0xFF000000U) >> 24;
		}

		[LuaMethodAttributes(
			"byteswap_64",
			"Byte swaps 'long'"
		)]
		public static UInt64 Byteswap_64(object _long)
		{
			UInt64 value = (UInt64)LuaLong(_long);
			return (value & 0x00000000000000FFUL) << 56 | (value & 0x000000000000FF00UL) << 40 |
		 (value & 0x0000000000FF0000UL) << 24 | (value & 0x00000000FF000000UL) << 8 |
		 (value & 0x000000FF00000000UL) >> 8 | (value & 0x0000FF0000000000UL) >> 24 |
		 (value & 0x00FF000000000000UL) >> 40 | (value & 0xFF00000000000000UL) >> 56;
		}
	}
}
