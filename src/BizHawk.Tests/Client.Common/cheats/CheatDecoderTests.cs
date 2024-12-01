using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using BizHawk.Client.Common;
using BizHawk.Client.Common.cheats;
using BizHawk.Emulation.Common;

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

			public string? GetDisplayName(MethodInfo methodInfo, object?[]? data)
			{
				static string Format(object? o) => o switch
				{
					null => "null",
					int i => $"0x{i:X}",
					string s => $"\"{s}\"",
					_ => o.ToString()!
				};
				return data is null ? null : $"{methodInfo.Name}({string.Join(", ", data.Select(Format))})";
			}
		}

		private const string ERROR_GBA_CODEBREAKER = "Codebreaker/GameShark SP/Xploder codes are not yet supported.";

		private static readonly int? NO_COMPARE = null;

		private static readonly IEnumerable<object?[]> NonsenseData = new[]
		{
			new[] { VSystemID.Raw.GBA, "33003D0E0020", ERROR_GBA_CODEBREAKER },
		};

		private static readonly IEnumerable<object?[]> RealData = new[]
		{
			new object?[] { VSystemID.Raw.GB, "0A1-B9F", 0x01B9, 0x0A, NO_COMPARE, WatchSize.Byte },
			new object?[] { VSystemID.Raw.GB, "068-5FF-E66", 0x085F, 0x06, 0x03, WatchSize.Byte },
			new object?[] { VSystemID.Raw.GB, "05D-49C-E62", 0x3D49, 0x05, 0x02, WatchSize.Byte },
			new object?[] { VSystemID.Raw.GB, "418-50B-91B", 0x4850, 0x41, 0x5C, WatchSize.Byte },
			new object?[] { VSystemID.Raw.GB, "508-4FB-800", 0x484F, 0x50, 0x9A, WatchSize.Byte },
			new object?[] { VSystemID.Raw.GB, "048-4EB-F76", 0x484E, 0x04, 0x07, WatchSize.Byte },
			new object?[] { VSystemID.Raw.GB, "BE0-37B-08C", 0x4037, 0xBE, 0xB9, WatchSize.Byte },
			new object?[] { VSystemID.Raw.GBA, "4012F5B7 3B7801A6", 0x00000006, 0xB7, NO_COMPARE, WatchSize.Byte },
			new object?[] { VSystemID.Raw.GBA, "686D7FC3 24B5B832", 0x00000032, 0x7FC3, NO_COMPARE, WatchSize.Word },
			new object?[] { VSystemID.Raw.SNES, "7E1F2801", 0x7E1F28, 0x01, NO_COMPARE, WatchSize.Byte },
		};

		[DataTestMethod]
		[CheatcodeData]
		public void TestCheatcodeParsing(string systemID, string code, int address, int value, int? compare, WatchSize size)
		{
			var result = new GameSharkDecoder(null, systemID).Decode(code);
			if (!result.IsValid(out var valid)) Assert.Fail($"failed to parse: {((InvalidCheatCode) result).Error}");
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
