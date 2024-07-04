using System.Buffers.Binary;
using System.ComponentModel;

// ReSharper disable UnusedMember.Global
namespace BizHawk.Client.Common
{
	[Description("A library for performing standard bitwise operations.")]
	public sealed class BitLuaLibrary : LuaLibraryBase
	{
		public BitLuaLibrary(ILuaLibraries luaLibsImpl, ApiContainer apiContainer, Action<string> logOutputCallback)
			: base(luaLibsImpl, apiContainer, logOutputCallback) {}

		public override string Name => "bit";

		[LuaDeprecatedMethod]
		[LuaMethodExample("local uibitban = bit.band( 1000, 4 );")]
		[LuaMethod("band", "Bitwise AND of 'val' against 'amt'")]
		public uint Band(uint val, uint amt)
		{
			Log("Using bit.band is deprecated, use the & operator instead.");
			return val & amt;
		}

		[LuaDeprecatedMethod]
		[LuaMethodExample("local uibitbno = bit.bnot( 1000 );")]
		[LuaMethod("bnot", "Bitwise NOT of 'val'")]
		public uint Bnot(uint val)
		{
			Log("Using bit.bnot is deprecated, use the ~a operator instead.");
			return ~val;
		}

		[LuaDeprecatedMethod]
		[LuaMethodExample("local uibitbor = bit.bor( 1000, 4 );")]
		[LuaMethod("bor", "Bitwise OR of 'val' against 'amt'")]
		public uint Bor(uint val, uint amt)
		{
			Log("Using bit.bor is deprecated, use the | operator instead.");
			return val | amt;
		}

		[LuaDeprecatedMethod]
		[LuaMethodExample("local uibitbxo = bit.bxor( 1000, 4 );")]
		[LuaMethod("bxor", "Bitwise XOR of 'val' against 'amt'")]
		public uint Bxor(uint val, uint amt)
		{
			Log("Using bit.bxor is deprecated, use the a ~ b operator instead (not a typo, ^ is pow).");
			return val ^ amt;
		}

		[LuaDeprecatedMethod]
		[LuaMethodExample("local uibitlsh = bit.lshift( 1000, 4 );")]
		[LuaMethod("lshift", "Logical shift left of 'val' by 'amt' bits")]
		public uint Lshift(uint val, int amt)
		{
			Log("Using bit.lshift is deprecated, use the << operator instead.");
			return val << amt;
		}

		[LuaMethodExample("local uibitrol = bit.rol( 1000, 4 );")]
		[LuaMethod("rol", "Left rotate 'val' by 'amt' bits")]
		public static uint Rol(uint val, int amt)
		{
			return (val << amt) | (val >> (32 - amt));
		}

		[LuaMethodExample("local uibitror = bit.ror( 1000, 4 );")]
		[LuaMethod("ror", "Right rotate 'val' by 'amt' bits")]
		public static uint Ror(uint val, int amt)
		{
			return (val >> amt) | (val << (32 - amt));
		}

		[LuaDeprecatedMethod]
		[LuaMethodExample("local uibitrsh = bit.rshift( 1000, 4 );")]
		[LuaMethod("rshift", "Logical shift right of 'val' by 'amt' bits")]
		public uint Rshift(uint val, int amt)
		{
			Log("Using bit.rshift is deprecated, use the >> operator instead.");
			return val >> amt;
		}

		[LuaMethodExample("local inbitars = bit.arshift( -1000, 4 );")]
		[LuaMethod("arshift", "Arithmetic shift right of 'val' by 'amt' bits")]
		public static int Arshift(uint val, int amt)
		{
			return (int)val >> amt;
		}

		[LuaMethodExample("if ( bit.check( -12345, 35 ) ) then\r\n\tconsole.log( \"Returns result of bit 'pos' being set in 'num'\" );\r\nend;")]
		[LuaMethod("check", "Returns result of bit 'pos' being set in 'num'")]
		public static bool Check(long num, int pos)
		{
			return (num & (1 << pos)) != 0;
		}

		[LuaMethodExample("local uibitset = bit.set( 25, 35 );")]
		[LuaMethod("set", "Sets the bit 'pos' in 'num'")]
		public static uint Set(uint num, int pos)
		{
			return num | 1U << pos;
		}

		[LuaMethodExample("local lobitcle = bit.clear( 25, 35 );")]
		[LuaMethod("clear", "Clears the bit 'pos' in 'num'")]
		public static long Clear(uint num, int pos)
		{
			return num & ~(1 << pos);
		}

		[LuaMethodExample("local usbitbyt = bit.byteswap_16( 100 );")]
		[LuaMethod("byteswap_16", "Byte swaps 'short', i.e. bit.byteswap_16(0xFF00) would return 0x00FF")]
		public static ushort Byteswap16(ushort val)
			=> BinaryPrimitives.ReverseEndianness(val);

		[LuaMethodExample("local uibitbyt = bit.byteswap_32( 1000 );")]
		[LuaMethod("byteswap_32", "Byte swaps 'dword'")]
		public static uint Byteswap32(uint val)
			=> BinaryPrimitives.ReverseEndianness(val);

		[LuaMethodExample("local ulbitbyt = bit.byteswap_64( 10000 );")]
		[LuaMethod("byteswap_64", "Byte swaps 'long'")]
		public static ulong Byteswap64(ulong val)
			=> BinaryPrimitives.ReverseEndianness(val);
	}
}
