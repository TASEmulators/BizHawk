using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Vectrex
{
	public partial class VectrexHawk : ISaveRam
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
			Buffer.BlockCopy(data, 0, cart_RAM, 0, data.Length);
			Console.WriteLine("loading SRAM here");
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
