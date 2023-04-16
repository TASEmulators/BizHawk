namespace BizHawk.Client.Common
{
	public interface ISaveStateApi : IExternalApi
	{
		void Load(string path, bool suppressOSD = false);
		void LoadSlot(int slotNum, bool suppressOSD = false);
		void Save(string path, bool suppressOSD = false);
		void SaveSlot(int slotNum, bool suppressOSD = false);
	}
}
