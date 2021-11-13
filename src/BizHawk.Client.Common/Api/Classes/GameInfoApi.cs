#nullable enable

using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public sealed class GameInfoApi : IGameInfoApi
	{
		[OptionalService]
		public IBoardInfo? _boardInfo { get; set; }

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
			foreach (var option in ((GameInfo) _game).GetOptions()) options[option.Key] = option.Value;
			return options;
		}
	}
}
