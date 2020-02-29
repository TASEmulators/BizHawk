using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common.cheats
{
	/// <summary>
	/// Represents a decoded cheat value
	/// </summary>
	public interface IDecodeResult
	{
		int Address { get; }
		int Value { get; }
		int? Compare { get; }
		WatchSize Size { get; }
		bool IsValid { get; }
		string Error { get; }
	}

	public class DecodeResult : IDecodeResult
	{
		public int Address { get; internal set; }
		public int Value { get; internal set; }
		public int? Compare { get; internal set; }
		public WatchSize Size { get; internal set; }
		public bool IsValid => true;
		public string Error => "";
	}

	public class InvalidResult : IDecodeResult
	{
		public InvalidResult(string error)
		{
			Error = error;
		}

		public int Address => int.MaxValue;
		public int Value => int.MaxValue;
		public int? Compare => null;
		public WatchSize Size => WatchSize.Separator;
		public bool IsValid => false;
		public string Error { get; }
	}

	public static class DecodeResultExtensions
	{
		public static Cheat ToCheat(this IDecodeResult result, MemoryDomain domain, string description)
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
