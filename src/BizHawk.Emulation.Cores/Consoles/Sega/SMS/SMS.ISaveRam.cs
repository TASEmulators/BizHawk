using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public partial class SMS : ISaveRam
	{
		public byte[] CloneSaveRam()
		{
			return (byte[]) SaveRAM?.Clone();
		}

		public void StoreSaveRam(byte[] data)
		{
			if (SaveRAM != null)
			{
				Array.Copy(data, SaveRAM, data.Length);
			}
		}

		public bool SaveRamModified { get; private set; }

		public byte[] SaveRAM;
		private byte SaveRamBank;
	}
}
