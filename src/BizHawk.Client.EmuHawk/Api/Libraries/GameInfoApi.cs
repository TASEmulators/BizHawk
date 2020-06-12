using System.Collections.Generic;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class GameInfoApi : IGameInfoApi
	{
		[OptionalService]
		private IBoardInfo BoardInfo { get; set; }

		public string GetRomName() => GlobalWin.Game?.Name ?? "";

		public string GetRomHash() => GlobalWin.Game?.Hash ?? "";

		public bool InDatabase() => GlobalWin.Game?.NotInDatabase == false;

		public string GetStatus() => GlobalWin.Game?.Status.ToString();

		public bool IsStatusBad() => GlobalWin.Game?.IsRomStatusBad() != false;

		public string GetBoardType() => BoardInfo?.BoardName ?? "";

		public Dictionary<string, string> GetOptions()
		{
			var options = new Dictionary<string, string>();
			if (GlobalWin.Game == null) return options;
			foreach (var option in GlobalWin.Game.GetOptions()) options[option.Key] = option.Value;
			return options;
		}
	}
}
