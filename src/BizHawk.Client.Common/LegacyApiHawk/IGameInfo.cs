#nullable enable

using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	[LegacyApiHawk]
	public interface IGameInfo : IExternalApi
	{
		[LegacyApiHawk]
		public string GetBoardType();

		[LegacyApiHawk]
		public Dictionary<string, string> GetOptions();

		[LegacyApiHawk]
		public string GetRomHash();

		[LegacyApiHawk]
		public string GetRomName();

		[LegacyApiHawk]
		public string? GetStatus();

		[LegacyApiHawk]
		public bool InDatabase();

		[LegacyApiHawk]
		public bool IsStatusBad();
	}
}
