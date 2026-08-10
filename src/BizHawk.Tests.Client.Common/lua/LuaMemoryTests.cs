using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Tests.Client.Common.Movie;

namespace BizHawk.Tests.Client.Common.lua
{
	[DoNotParallelize]
	[TestClass]
	public class LuaMemoryTests
	{
		[TestMethod]
		public void Test_read_u16_le_as_array()
		{
			// arrange
			LuaTestContext context = new();
			context.AddScript("memory/read_u16_le_as_array.lua", true);
			ushort[] memUshorts = new ushort[FakeEmulator.InitialMemory.Length / sizeof(ushort)];
			FakeEmulator.InitialMemory.CopyTo(memUshorts.BytesSpan());

			// act
			context.RunYielding();

			// assert
			context.AssertLogMatches(memUshorts[0].ToString(), memUshorts[1].ToString());
		}

		[TestMethod]
		public void Test_read_u16_be_as_array()
		{
			// arrange
			LuaTestContext context = new();
			context.AddScript("memory/read_u16_be_as_array.lua", true);
			ushort[] memUshorts = new ushort[FakeEmulator.InitialMemory.Length / sizeof(ushort)];
			FakeEmulator.InitialMemory.CopyTo(memUshorts.BytesSpan());
			EndiannessUtils.MutatingByteSwap16(memUshorts.BytesSpan());

			// act
			context.RunYielding();

			// assert
			context.AssertLogMatches(memUshorts[0].ToString(), memUshorts[1].ToString());
		}

		[TestMethod]
		public void Test_write_u16_le_as_array() => LuaTestContext.RunTestFromLuaScript("memory/write_u16_le_as_array.lua");
		[TestMethod]
		public void Test_write_u16_be_as_array() => LuaTestContext.RunTestFromLuaScript("memory/write_u16_be_as_array.lua");

		[TestMethod]
		public void Test_read_u32_le_as_array()
		{
			// arrange
			LuaTestContext context = new();
			context.AddScript("memory/read_u32_le_as_array.lua", true);
			uint[] memUints = new uint[FakeEmulator.InitialMemory.Length / sizeof(uint)];
			FakeEmulator.InitialMemory.CopyTo(memUints.BytesSpan());

			// act
			context.RunYielding();

			// assert
			context.AssertLogMatches(memUints[0].ToString(), memUints[1].ToString());
		}

		[TestMethod]
		public void Test_read_u32_be_as_array()
		{
			// arrange
			LuaTestContext context = new();
			context.AddScript("memory/read_u32_be_as_array.lua", true);
			uint[] memUints = new uint[FakeEmulator.InitialMemory.Length / sizeof(uint)];
			FakeEmulator.InitialMemory.CopyTo(memUints.BytesSpan());
			EndiannessUtils.MutatingByteSwap32(memUints.BytesSpan());

			// act
			context.RunYielding();

			// assert
			context.AssertLogMatches(memUints[0].ToString(), memUints[1].ToString());
		}

		[TestMethod]
		public void Test_write_u32_le_as_array() => LuaTestContext.RunTestFromLuaScript("memory/write_u32_le_as_array.lua");
		[TestMethod]
		public void Test_write_u32_be_as_array() => LuaTestContext.RunTestFromLuaScript("memory/write_u32_be_as_array.lua");
	}
}
