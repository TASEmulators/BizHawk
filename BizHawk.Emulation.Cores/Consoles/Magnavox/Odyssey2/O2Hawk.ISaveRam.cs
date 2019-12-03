using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.O2Hawk
{
	public partial class O2Hawk : ISaveRam
	{
		public byte[] CloneSaveRam()
		{
			if (cart_RAM != null)
			{
				return (byte[])cart_RAM.Clone();
			}
			else
			{
				return null;
			}
		}

		public void StoreSaveRam(byte[] data)
		{
			if (_syncSettings.Use_SRAM)
			{
				Buffer.BlockCopy(data, 0, cart_RAM, 0, data.Length);
				Console.WriteLine("loading SRAM here");
			}
		}

		public bool SaveRamModified
		{
			get 
			{
				return has_bat & _syncSettings.Use_SRAM;
			}	
		}
	}
}
