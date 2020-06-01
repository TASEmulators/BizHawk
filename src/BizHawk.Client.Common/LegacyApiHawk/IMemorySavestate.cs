#nullable enable

namespace BizHawk.Client.Common
{
	[LegacyApiHawk]
	public interface IMemorySaveState : IExternalApi
	{
		[LegacyApiHawk]
		void ClearInMemoryStates();

		[LegacyApiHawk]
		void DeleteState(string identifier);

		[LegacyApiHawk]
		void LoadCoreStateFromMemory(string identifier);

		[LegacyApiHawk]
		string SaveCoreStateToMemory();
	}
}
