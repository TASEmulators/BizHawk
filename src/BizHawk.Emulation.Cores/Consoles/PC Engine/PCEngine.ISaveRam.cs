using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.PCEngine
{
	public sealed partial class PCEngine : ISaveRam
	{
		public bool SaveRamModified { get; private set; }

		public bool SupportsSaveRam => BRAM != null;

		public byte[] CloneSaveRam(bool clearDirty)
		{
			if (clearDirty) SaveRamModified = false;
			return (byte[]) BRAM?.Clone();
		}

		public void StoreSaveRam(byte[] data)
		{
			if (BRAM != null)
			{
				if (data.Length != BRAM.Length) throw new InvalidOperationException("Incorrect sram size.");
				Array.Copy(data, BRAM, data.Length);
			}

			SaveRamModified = false;
		}
	}
}
