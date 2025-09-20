using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public partial class SMS : ISaveRam
	{
		public byte[] CloneSaveRam(bool clearDirty)
		{
			if (SaveRAM == null) throw new InvalidOperationException("Core currently has no SRAM and should not be providing ISaveRam service.");
			if (clearDirty) SaveRamModified = false;
			return (byte[]) SaveRAM.Clone();
		}

		public void StoreSaveRam(byte[] data)
		{
			if (SaveRAM != null)
			{
				if (data.Length != SaveRAM.Length) throw new InvalidOperationException("Incorrect sram size.");
				Array.Copy(data, SaveRAM, data.Length);
			}
			else
			{
				throw new InvalidOperationException("Core currently has no SRAM and should not be providing ISaveRam service.");
			}

			SaveRamModified = false;
		}

		public bool SaveRamModified { get; private set; }

		public byte[] SaveRAM;
		private byte SaveRamBank;
	}
}
