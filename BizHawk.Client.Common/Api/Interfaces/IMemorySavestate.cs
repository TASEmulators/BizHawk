namespace BizHawk.Client.Common
{
	public interface IMemorySaveState : IExternalApi
	{
		string SaveCoreStateToMemory();
		void LoadCoreStateFromMemory(string identifier);
		void DeleteState(string identifier);
		void ClearInMemoryStates();
	}
}
