using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.MSX
{
	public partial class MSX : ISaveRam
	{
		public byte[] CloneSaveRam(bool clearDirty)
		{
			if (SaveRAM == null)
				throw new InvalidOperationException("Core currently has no SRAM and should not be providing ISaveRam service.");
			else
				return (byte[]) SaveRAM.Clone();
		}

		public void StoreSaveRam(byte[] data)
		{
			if (SaveRAM == null)
				throw new InvalidOperationException("Core currently has no SRAM and should not be providing ISaveRam service.");
			else
			{
				if (data.Length != SaveRAM.Length) throw new InvalidOperationException("Incorrect sram size.");
				Array.Copy(data, SaveRAM, data.Length);
			}
		}

		public bool SaveRamModified { get; private set; }

		private byte[] SaveRAM;
		private byte SaveRamBank;
	}
}
