#nullable enable

using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	[Obsolete("use IEmulationApi")]
	public interface IGameInfoApi : IExternalApi
	{
		string GetBoardType();

		IGameInfo? GetGameInfo();

		IReadOnlyDictionary<string, string?> GetOptions();
	}
}
