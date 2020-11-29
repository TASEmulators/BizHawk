using System.Collections.Generic;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class GameInfoApi : IGameInfoApi
	{
		[OptionalService]
		private IBoardInfo BoardInfo { get; set; }

		private readonly IGameInfo _game;

		public GameInfoApi(IGameInfo game) => _game = game;

		public string GetRomName() => _game?.Name ?? "";

		public string GetRomHash() => _game?.Hash ?? "";

		public bool InDatabase() => _game?.NotInDatabase == false;

		public string GetStatus() => _game?.Status.ToString();

		public bool IsStatusBad() => _game?.IsRomStatusBad() != false;

		public string GetBoardType() => BoardInfo?.BoardName ?? "";

		public Dictionary<string, string> GetOptions()
		{
			var options = new Dictionary<string, string>();
			if (_game == null) return options;
			foreach (var option in ((GameInfo) _game).GetOptions()) options[option.Key] = option.Value;
			return options;
		}
	}
}
