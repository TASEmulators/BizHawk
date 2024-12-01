using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawkLink3x
{
	public partial class GBHawkLink3x : ISaveRam
	{
		public byte[] CloneSaveRam()
		{
			if (L.cart_RAM != null || C.cart_RAM != null || R.cart_RAM != null)
			{
				int len1 = 0;
				int len2 = 0;
				int len3 = 0;
				int index = 0;

				if (L.cart_RAM != null)
				{
					len1 = L.cart_RAM.Length;
				}

				if (C.cart_RAM != null)
				{
					len2 = C.cart_RAM.Length;
				}

				if (R.cart_RAM != null)
				{
					len3 = R.cart_RAM.Length;
				}

				byte[] temp = new byte[len1 + len2 + len3];

				if (L.cart_RAM != null)
				{
					for (int i = 0; i < L.cart_RAM.Length; i++)
					{
						temp[index] = L.cart_RAM[i];
						index++;
					}
				}

				if (C.cart_RAM != null)
				{
					for (int i = 0; i < C.cart_RAM.Length; i++)
					{
						temp[index] = C.cart_RAM[i];
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
			if (Link3xSyncSettings.Use_SRAM)
			{
				int temp = 0;

				if (L.cart_RAM != null)
				{
					Buffer.BlockCopy(data, temp, L.cart_RAM, 0, L.cart_RAM.Length);
					temp += L.cart_RAM.Length;
				}

				if (C.cart_RAM != null)
				{
					Buffer.BlockCopy(data, temp, C.cart_RAM, 0, C.cart_RAM.Length);
					temp += C.cart_RAM.Length;
				}

				if (R.cart_RAM != null)
				{
					Buffer.BlockCopy(data, temp, R.cart_RAM, 0, R.cart_RAM.Length);
				}

				Console.WriteLine("loading SRAM here");
			}
		}

		public bool SaveRamModified => (L.has_bat || C.has_bat || R.has_bat) & Link3xSyncSettings.Use_SRAM;
	}
}
