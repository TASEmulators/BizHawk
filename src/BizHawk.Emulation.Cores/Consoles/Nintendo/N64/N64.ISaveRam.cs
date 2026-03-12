using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.N64
{
	public partial class N64 : ISaveRam
	{
		public byte[] CloneSaveRam(bool clearDirty)
		{
			return api.SaveSaveram();
		}

		public void StoreSaveRam(byte[] data)
		{
			api.LoadSaveram(data);
		}

		public bool SaveRamModified => true;
	}
}
