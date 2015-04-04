using System;
using System.ComponentModel;

using LuaInterface;

namespace BizHawk.Client.Common
{
	[Description("A library for performing standard bitwise operations.")]
	public sealed class BitLuaLibrary : LuaLibraryBase
	{
		public BitLuaLibrary(Lua lua)
			: base(lua) { }

		public BitLuaLibrary(Lua lua, Action<string> logOutputCallback)
			: base(lua, logOutputCallback) { }

		public override string Name { get { return "bit"; } }

		[LuaMethodAttributes(
			"band",
			"Bitwise AND of 'val' against 'amt'"
		)]
		public static uint Band(uint val, uint amt)
		{
			return val & amt;
		}

		[LuaMethodAttributes(
			"bnot",
			"Bitwise NOT of 'val' against 'amt'"
		)]
		public static uint Bnot(uint val)
		{
			return ~val;
		}

		[LuaMethodAttributes(
			"bor",
			"Bitwise OR of 'val' against 'amt'"
		)]
		public static uint Bor(uint val, uint amt)
		{
			return val | amt;
		}

		[LuaMethodAttributes(
			"bxor",
			"Bitwise XOR of 'val' against 'amt'"
		)]
		public static uint Bxor(uint val, uint amt)
		{
			return val ^ amt;
		}

		[LuaMethodAttributes(
			"lshift",
			"Logical shift left of 'val' by 'amt' bits"
		)]
		public static uint Lshift(uint val, int amt)
		{
			return val << amt;
		}

		[LuaMethodAttributes(
			"rol",
			"Left rotate 'val' by 'amt' bits"
		)]
		public static uint Rol(uint val, int amt)
		{
			return (val << amt) | (val >> (32 - amt));
		}

		[LuaMethodAttributes(
			"ror",
			"Right rotate 'val' by 'amt' bits"
		)]
		public static uint Ror(uint val, int amt)
		{
			return (val >> amt) | (val << (32 - amt));
		}

		[LuaMethodAttributes(
			"rshift",
			"Logical shift right of 'val' by 'amt' bits"
		)]
		public static uint Rshift(uint val, int amt)
		{
			return (uint)(val >> amt);
		}

		[LuaMethodAttributes(
			"arshift",
			"Arithmetic shift right of 'val' by 'amt' bits"
		)]
		public static int Arshift(int val, int amt)
		{
			return (int)(val >> amt);
		}

		[LuaMethodAttributes(
			"check",
			"Returns result of bit 'pos' being set in 'num'"
		)]
		public static bool Check(long num, int pos)
		{
			return (num & (1 << pos)) != 0;
		}

		[LuaMethodAttributes(
			"set",
			"Sets the bit 'pos' in 'num'"
		)]
		public static uint Set(uint num, int pos)
		{
			return (uint)(num | (uint)1 << pos);
		}

		[LuaMethodAttributes(
			"clear",
			"Clears the bit 'pos' in 'num'"
		)]
		public static long Clear(uint num, int pos)
		{
			return num & ~(1 << pos);
		}

		[LuaMethodAttributes(
			"byteswap_16",
			"Byte swaps 'short', i.e. bit.byteswap_16(0xFF00) would return 0x00FF"
		)]
		public static ushort Byteswap16(ushort val)
		{
			return (ushort)((val & 0xFFU) << 8 | (val & 0xFF00U) >> 8);
		}

		[LuaMethodAttributes(
			"byteswap_32",
			"Byte swaps 'dword'"
		)]
		public static uint Byteswap32(uint val)
		{
			return (val & 0x000000FFU) << 24 | (val & 0x0000FF00U) << 8 |
				(val & 0x00FF0000U) >> 8 | (val & 0xFF000000U) >> 24;
		}

		[LuaMethodAttributes(
			"byteswap_64",
			"Byte swaps 'long'"
		)]
		public static ulong Byteswap64(ulong val)
		{
			return (val & 0x00000000000000FFUL) << 56 | (val & 0x000000000000FF00UL) << 40 |
				(val & 0x0000000000FF0000UL) << 24 | (val & 0x00000000FF000000UL) << 8 |
				(val & 0x000000FF00000000UL) >> 8 | (val & 0x0000FF0000000000UL) >> 24 |
				(val & 0x00FF000000000000UL) >> 40 | (val & 0xFF00000000000000UL) >> 56;
		}
	}
}
