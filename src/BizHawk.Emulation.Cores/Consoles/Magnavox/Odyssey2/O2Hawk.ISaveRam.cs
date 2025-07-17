using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.O2Hawk
{
	public partial class O2Hawk : ISaveRam
	{
		public byte[] CloneSaveRam(bool clearDirty)
		{
			return (byte[])cart_RAM?.Clone();
		}

		public void StoreSaveRam(byte[] data)
		{
			if (cart_RAM != null && _syncSettings.Use_SRAM)
			{
				if (data.Length != cart_RAM.Length) throw new InvalidOperationException("Incorrect sram size.");
				Buffer.BlockCopy(data, 0, cart_RAM, 0, data.Length);
				Console.WriteLine("loading SRAM here");
			}
		}

		public bool SaveRamModified => has_bat & _syncSettings.Use_SRAM;

		public bool SupportsSaveRam => cart_RAM != null; // The Use_SRAM setting implements behavior not officially supported by BizHawk.
	}
}
