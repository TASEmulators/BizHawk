using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawkLink
{
	public partial class GBHawkLink : ISaveRam
	{
		public byte[] CloneSaveRam()
		{
			if (L.cart_RAM != null)
			{
				return (byte[])L.cart_RAM.Clone();
			}
			else
			{
				return null;
			}
		}

		public void StoreSaveRam(byte[] data)
		{
			Buffer.BlockCopy(data, 0, L.cart_RAM, 0, data.Length);
			Console.WriteLine("loading SRAM here");
		}

		public bool SaveRamModified
		{
			get 
			{
				return L.has_bat & _syncSettings.L.Use_SRAM;
			}	
		}
	}
}
