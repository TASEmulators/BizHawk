using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public interface IGameInfo : IExternalApi
	{
		string BoardType { get; }

		bool IsStatusBad { get; }

		string RomHash { get; }

		string RomName { get; }

		bool RomNotInDatabase { get; }

		string RomStatus { get; }

		IDictionary<string, string> GetOptions();
	}
}
