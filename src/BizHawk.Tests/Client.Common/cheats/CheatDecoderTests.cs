using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using BizHawk.Client.Common;
using BizHawk.Client.Common.cheats;

namespace BizHawk.Tests.Client.Common.cheats
{
	[TestClass]
	public class CheatDecoderTests
	{
		[AttributeUsage(AttributeTargets.Method)]
		private sealed class CheatcodeDataAttribute : Attribute, ITestDataSource
		{
			public bool GenerateNonsense { get; set; } = false;

			public IEnumerable<object?[]> GetData(MethodInfo methodInfo)
				=> GenerateNonsense ? NonsenseData : RealData;

			public string GetDisplayName(MethodInfo methodInfo, object?[] data)
			{
				static string Format(object? o) => o switch
				{
					null => "null",
					int i => $"0x{i:X}",
					string s => $"\"{s}\"",
					_ => o.ToString()!
				};
				return $"{methodInfo.Name}({string.Join(", ", data.Select(Format))})";
			}
		}

		private const string ERROR_GBA_CODEBREAKER = "Codebreaker/GameShark SP/Xploder codes are not yet supported.";

		private static readonly int? NO_COMPARE = null;

		private static readonly IEnumerable<object?[]> NonsenseData = new[]
		{
			new[] { "GBA", "33003D0E0020", ERROR_GBA_CODEBREAKER },
		};

		private static readonly IEnumerable<object?[]> RealData = new[]
		{
			new object?[] { "GBA", "4012F5B7 3B7801A6", 0x00000006, 0xB7, NO_COMPARE, WatchSize.Byte },
			new object?[] { "GBA", "686D7FC3 24B5B832", 0x00000032, 0x7FC3, NO_COMPARE, WatchSize.Word },
		};

		[DataTestMethod]
		[CheatcodeData]
		public void TestCheatcodeParsing(string systemID, string code, int address, int value, int? compare, WatchSize size)
		{
			var result = new GameSharkDecoder(null, systemID).Decode(code);
			Assert.IsTrue(result.IsValid(out var valid), "failed to parse");
			Assert.AreEqual(address, valid.Address, "wrong addr");
			Assert.AreEqual(size, valid.Size, "wrong size");
			Assert.AreEqual(
				value,
				valid.Size switch
				{
					WatchSize.Byte => valid.Value & 0xFF,
					WatchSize.Word => valid.Value & 0xFFFF,
					_ => valid.Value
				},
				"wrong value");
			Assert.AreEqual(compare, valid.Compare, "wrong compare");
		}

		[DataTestMethod]
		[CheatcodeData(GenerateNonsense = true)]
		public void TestNonsenseParsing(string systemID, string code, string error)
		{
			var result = new GameSharkDecoder(null, systemID).Decode(code);
			Assert.IsFalse(result.IsValid(out _), "parsed unexpectedly");
			Assert.AreEqual(error, result.Error, "wrong error msg");
		}
	}
}
