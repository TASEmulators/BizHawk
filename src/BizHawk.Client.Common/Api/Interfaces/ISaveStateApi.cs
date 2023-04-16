namespace BizHawk.Client.Common
{
	public interface ISaveStateApi : IExternalApi
	{
		/// <param name="path">absolute path to <c>.State</c> file</param>
		/// <returns><see langword="true"/> iff succeeded</returns>
		bool Load(string path, bool suppressOSD = false);

		/// <param name="slotNum"><c>1..10</c></param>
		/// <returns><see langword="true"/> iff succeeded</returns>
		bool LoadSlot(int slotNum, bool suppressOSD = false);

		void Save(string path, bool suppressOSD = false);
		void SaveSlot(int slotNum, bool suppressOSD = false);
	}
}
