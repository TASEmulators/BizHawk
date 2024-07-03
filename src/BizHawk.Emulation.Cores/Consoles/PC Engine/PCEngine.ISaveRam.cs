using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.PCEngine
{
	public sealed partial class PCEngine : ISaveRam
	{
		public bool SaveRamModified { get; private set; }

		public byte[] CloneSaveRam()
		{
			return (byte[]) BRAM?.Clone();
		}

		public void StoreSaveRam(byte[] data)
		{
			if (BRAM != null)
			{
				Array.Copy(data, BRAM, data.Length);
			}
		}
	}
}
