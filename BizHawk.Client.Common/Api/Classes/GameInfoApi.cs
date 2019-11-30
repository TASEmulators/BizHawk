using System.Collections.Generic;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public sealed class GameInfoApi : IGameInfo
	{
		[OptionalService]
		private IBoardInfo BoardInfo { get; set; }

		public string GetRomName()
		{
			return Global.Game?.Name ?? "";
		}

		public string GetRomHash()
		{
			return Global.Game?.Hash ?? "";
		}

		public bool InDatabase()
		{
			if (Global.Game != null)
			{
				return !Global.Game.NotInDatabase;
			}

			return false;
		}

		public string GetStatus()
		{
			return Global.Game?.Status.ToString();
		}

		public bool IsStatusBad()
		{
			return Global.Game?.IsRomStatusBad() ?? true;
		}

		public string GetBoardType()
		{
			return BoardInfo?.BoardName ?? "";
		}

		public Dictionary<string, string> GetOptions()
		{
			var options = new Dictionary<string, string>();

			if (Global.Game != null)
			{
				foreach (var option in Global.Game.GetOptionsDict())
				{
					options[option.Key] = option.Value;
				}
			}

			return options;
		}
	}
}
