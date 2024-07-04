using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	public partial class GBHawk : ISaveRam
	{
		public byte[] CloneSaveRam()
		{
			return (byte[])cart_RAM?.Clone();
		}

		public void StoreSaveRam(byte[] data)
		{
			if (_syncSettings.Use_SRAM)
			{
				Buffer.BlockCopy(data, 0, cart_RAM, 0, data.Length);
				Console.WriteLine("loading SRAM here");
			}
		}

		public bool SaveRamModified => has_bat & _syncSettings.Use_SRAM;
	}
}
