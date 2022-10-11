#nullable enable

using System;
using System.Collections.Generic;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	[GenEmuServiceProp(typeof(IBoardInfo), "_boardInfo", IsOptional = true)]
	[Obsolete("use IEmulationApi")]
	public sealed partial class GameInfoApi : IGameInfoApi
	{
		private readonly IGameInfo? _game;

		public GameInfoApi(IGameInfo? game)
			=> _game = game;

		public string GetBoardType()
			=> _boardInfo?.BoardName ?? string.Empty;

		public IGameInfo? GetGameInfo()
			=> _game;

		public IReadOnlyDictionary<string, string?> GetOptions()
		{
			var options = new Dictionary<string, string?>();
			if (_game == null) return options;
			foreach (var (k, v) in ((GameInfo) _game).GetOptions()) options[k] = v;
			return options;
		}
	}
}
