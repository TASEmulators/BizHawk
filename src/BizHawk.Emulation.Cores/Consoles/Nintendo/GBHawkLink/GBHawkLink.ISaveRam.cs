using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawkLink
{
	public partial class GBHawkLink : ISaveRam
	{
		public byte[] CloneSaveRam()
		{
			if (L.cart_RAM != null || R.cart_RAM != null)
			{
				int len1 = 0;
				int len2 = 0;
				int index = 0;

				if (L.cart_RAM != null)
				{
					len1 = L.cart_RAM.Length;
				}

				if (R.cart_RAM != null)
				{
					len2 = R.cart_RAM.Length;
				}

				byte[] temp = new byte[len1 + len2];

				if (L.cart_RAM != null)
				{
					for (int i = 0; i < L.cart_RAM.Length; i++)
					{
						temp[index] = L.cart_RAM[i];
						index++;
					}
				}

				if (R.cart_RAM != null)
				{
					for (int i = 0; i < R.cart_RAM.Length; i++)
					{
						temp[index] = R.cart_RAM[i];
						index++;
					}
				}

				return temp;
			}

			return null;
		}

		public void StoreSaveRam(byte[] data)
		{
			if (linkSyncSettings.Use_SRAM)
			{
				if (L.cart_RAM != null && R.cart_RAM == null)
				{
					Buffer.BlockCopy(data, 0, L.cart_RAM, 0, L.cart_RAM.Length);
				}
				else if (R.cart_RAM != null && L.cart_RAM == null)
				{
					Buffer.BlockCopy(data, 0, R.cart_RAM, 0, R.cart_RAM.Length);
				}
				else if (R.cart_RAM != null && L.cart_RAM != null)
				{
					Buffer.BlockCopy(data, 0, L.cart_RAM, 0, L.cart_RAM.Length);
					Buffer.BlockCopy(data, L.cart_RAM.Length, R.cart_RAM, 0, R.cart_RAM.Length);
				}

				Console.WriteLine("loading SRAM here");
			}
		}

		public bool SaveRamModified => (L.has_bat || R.has_bat) & linkSyncSettings.Use_SRAM;
	}
}
