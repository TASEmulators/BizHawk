using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common.cheats
{
	/// <summary>
	/// Represents a decoded cheat value
	/// </summary>
	public interface IDecodeResult
	{
		string Error { get; }
	}

	public class DecodeResult : IDecodeResult
	{
		public int Address { get; internal set; }
		public int Value { get; internal set; }
		public int? Compare { get; internal set; }
		public WatchSize Size { get; internal set; }
		public string Error => "";
	}

	public class InvalidCheatCode : IDecodeResult
	{
		public InvalidCheatCode(string error)
		{
			Error = error;
		}

		public string Error { get; }
	}

	public static class DecodeResultExtensions
	{
		public static bool IsValid(this IDecodeResult result, out DecodeResult valid)
		{
			valid = result as DecodeResult;
			return result is DecodeResult;
		}

		public static Cheat ToCheat(this DecodeResult result, MemoryDomain domain, string description)
		{
			var watch = Watch.GenerateWatch(
				domain,
				result.Address,
				result.Size,
				DisplayType.Hex,
				domain.EndianType == MemoryDomain.Endian.Big,
				description);
			return result.Compare.HasValue
				? new Cheat(watch, result.Value, result.Compare.Value, true, Cheat.CompareType.Equal)
				: new Cheat(watch, result.Value);
		}
	}
}
