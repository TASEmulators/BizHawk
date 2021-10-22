#nullable enable

using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface IGameInfoApi : IExternalApi
	{
		string GetBoardType();

		IGameInfo? GetGameInfo();

		IReadOnlyDictionary<string, string?> GetOptions();
	}
}
