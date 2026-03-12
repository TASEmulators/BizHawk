using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public partial class SMS : ISaveRam
	{
		public byte[] CloneSaveRam(bool clearDirty)
		{
			if (clearDirty) SaveRamModified = false;
			return (byte[]) SaveRAM?.Clone();
		}

		public void StoreSaveRam(byte[] data)
		{
			if (SaveRAM != null)
			{
				Array.Copy(data, SaveRAM, data.Length);
			}

			SaveRamModified = false;
		}

		public bool SaveRamModified { get; private set; }

		public byte[] SaveRAM;
		private byte SaveRamBank;
	}
}
