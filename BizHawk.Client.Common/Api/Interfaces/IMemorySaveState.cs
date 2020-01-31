using System;

namespace BizHawk.Client.Common
{
	public interface IMemorySaveState : IExternalApi
	{
		void ClearInMemoryStates();

		void DeleteState(Guid guid);

		void LoadCoreStateFromMemory(Guid guid);

		Guid SaveCoreStateToMemory();
	}
}
