namespace BizHawk.Client.Common
{
	public interface ISaveStateApi : IExternalApi
	{
		bool Load(string path, bool suppressOSD = false);

		bool LoadSlot(int slotNum, bool suppressOSD = false);

		void Save(string path, bool suppressOSD = false);
		void SaveSlot(int slotNum, bool suppressOSD = false);
	}
}
