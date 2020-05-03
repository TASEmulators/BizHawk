using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public sealed class GameInfoApi : IGameInfo
	{
		[OptionalService]
		private IBoardInfo BoardInfo { get; set; }

		public string GetRomName() => Global.Game?.Name ?? "";

		public string GetRomHash() => Global.Game?.Hash ?? "";

		public bool InDatabase() => Global.Game?.NotInDatabase == false;

		public string GetStatus() => Global.Game?.Status.ToString();

		public bool IsStatusBad() => Global.Game?.IsRomStatusBad() != false;

		public string GetBoardType() => BoardInfo?.BoardName ?? "";

		public Dictionary<string, string> GetOptions()
		{
			var options = new Dictionary<string, string>();
			if (Global.Game == null) return options;
			foreach (var option in Global.Game.GetOptionsDict()) options[option.Key] = option.Value;
			return options;
		}
	}
}
