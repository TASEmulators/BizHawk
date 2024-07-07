using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawkLink4x
{
	public partial class GBHawkLink4x : ISaveRam
	{
		public byte[] CloneSaveRam()
		{
			if (A.cart_RAM != null || B.cart_RAM != null || C.cart_RAM != null || D.cart_RAM != null)
			{
				int len1 = 0;
				int len2 = 0;
				int len3 = 0;
				int len4 = 0;
				int index = 0;

				if (A.cart_RAM != null)
				{
					len1 = A.cart_RAM.Length;
				}

				if (B.cart_RAM != null)
				{
					len2 = B.cart_RAM.Length;
				}

				if (C.cart_RAM != null)
				{
					len3 = C.cart_RAM.Length;
				}

				if (D.cart_RAM != null)
				{
					len4 = D.cart_RAM.Length;
				}

				byte[] temp = new byte[len1 + len2 + len3 + len4];

				if (A.cart_RAM != null)
				{
					for (int i = 0; i < A.cart_RAM.Length; i++)
					{
						temp[index] = A.cart_RAM[i];
						index++;
					}
				}

				if (B.cart_RAM != null)
				{
					for (int i = 0; i < B.cart_RAM.Length; i++)
					{
						temp[index] = B.cart_RAM[i];
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

				if (D.cart_RAM != null)
				{
					for (int i = 0; i < D.cart_RAM.Length; i++)
					{
						temp[index] = D.cart_RAM[i];
						index++;
					}
				}

				return temp;
			}

			return null;
		}

		public void StoreSaveRam(byte[] data)
		{
			if (Link4xSyncSettings.Use_SRAM)
			{
				int temp = 0;

				if (A.cart_RAM != null)
				{
					Buffer.BlockCopy(data, temp, A.cart_RAM, 0, A.cart_RAM.Length);
					temp += A.cart_RAM.Length;
				}

				if (B.cart_RAM != null)
				{
					Buffer.BlockCopy(data, temp, B.cart_RAM, 0, B.cart_RAM.Length);
					temp += B.cart_RAM.Length;
				}

				if (C.cart_RAM != null)
				{
					Buffer.BlockCopy(data, temp, C.cart_RAM, 0, C.cart_RAM.Length);
					temp += C.cart_RAM.Length;
				}

				if (D.cart_RAM != null)
				{
					Buffer.BlockCopy(data, temp, D.cart_RAM, 0, D.cart_RAM.Length);
				}

				Console.WriteLine("loading SRAM here");
			}
		}

		public bool SaveRamModified => (A.has_bat || B.has_bat || C.has_bat || D.has_bat) & Link4xSyncSettings.Use_SRAM;
	}
}
