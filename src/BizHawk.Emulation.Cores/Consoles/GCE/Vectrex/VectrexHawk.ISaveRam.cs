//using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Vectrex
{
	public partial class VectrexHawk// : ISaveRam
	{
		public byte[] CloneSaveRam(bool clearDirty)
		{
			return null;
		}

		public void StoreSaveRam(byte[] data) {}

		public bool SaveRamModified => false;
	}
}
