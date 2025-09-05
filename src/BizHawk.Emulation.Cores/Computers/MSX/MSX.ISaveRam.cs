using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.MSX
{
	public partial class MSX : ISaveRam
	{
		public byte[] CloneSaveRam(bool clearDirty)
		{
			return (byte[]) SaveRAM?.Clone();
		}

		public void StoreSaveRam(byte[] data)
		{
			if (SaveRAM != null)
			{
				if (data.Length != SaveRAM.Length) throw new InvalidOperationException("Incorrect sram size.");
				Array.Copy(data, SaveRAM, data.Length);
			}
		}

		public bool SaveRamModified { get; private set; }

		public bool SupportsSaveRam => SaveRAM != null;

		public byte[] SaveRAM;
		private byte SaveRamBank;
	}
}
