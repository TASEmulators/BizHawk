using System.Collections.Generic;

using BizHawk.Common.CollectionExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public sealed class GameInfoApi : IGameInfo
	{
		[OptionalService]
		private IBoardInfo BoardInfo { get; set; }

		public string BoardType => BoardInfo?.BoardName ?? string.Empty;

		public bool IsStatusBad => Global.Game?.IsRomStatusBad() != false;

		public string RomHash => Global.Game?.Hash ?? string.Empty;

		public string RomName => Global.Game?.Name ?? string.Empty;

		public bool RomNotInDatabase => Global.Game?.NotInDatabase != false;

		public string RomStatus => Global.Game?.Status.ToString();

		public IDictionary<string, string> GetOptions() => Global.Game?.GetOptionsDict()?.SimpleCopy() ?? new Dictionary<string, string>();
	}
}
