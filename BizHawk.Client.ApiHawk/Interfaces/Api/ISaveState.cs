namespace BizHawk.Client.ApiHawk
{
	public interface ISaveState : IExternalApi
	{
		void Load(string path);
		void LoadSlot(int slotNum);
		void Save(string path);
		void SaveSlot(int slotNum);
	}
}
