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
			if (Global.Game != null)
			{
				return Global.Game.Name ?? "";
			}

			return "";
		}

		public string GetRomHash()
		{
			if (Global.Game != null)
			{
				return Global.Game.Hash ?? "";
			}

			return "";
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
			if (Global.Game != null)
			{
				return Global.Game.Status.ToString();
			}

			return "";
		}

		public bool IsStatusBad()
		{
			if (Global.Game != null)
			{
				return Global.Game.IsRomStatusBad();
			}

			return true;
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
