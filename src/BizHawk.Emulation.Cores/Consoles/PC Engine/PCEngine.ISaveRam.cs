using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.PCEngine
{
	public sealed partial class PCEngine : ISaveRam
	{
		public bool SaveRamModified { get; private set; }

		public byte[] CloneSaveRam(bool clearDirty)
		{
			if (BRAM == null) throw new InvalidOperationException("Core currently has no SRAM and should not be providing ISaveRam service.");
			if (clearDirty) SaveRamModified = false;
			return (byte[]) BRAM.Clone();
		}

		public void StoreSaveRam(byte[] data)
		{
			if (BRAM != null)
			{
				if (data.Length != BRAM.Length) throw new InvalidOperationException("Incorrect sram size.");
				Array.Copy(data, BRAM, data.Length);
			}
			else
			{
				throw new InvalidOperationException("Core currently has no SRAM and should not be providing ISaveRam service.");
			}

			SaveRamModified = false;
		}
	}
}
