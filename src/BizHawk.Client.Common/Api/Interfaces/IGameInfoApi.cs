using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public interface IGameInfoApi : IExternalApi
	{
		string GetRomName();
		string GetRomHash();
		bool InDatabase();
		string GetStatus();
		bool IsStatusBad();
		string GetBoardType();
		IReadOnlyDictionary<string, string> GetOptions();
	}
}
