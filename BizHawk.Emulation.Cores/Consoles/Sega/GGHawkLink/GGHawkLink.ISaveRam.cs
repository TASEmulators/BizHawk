using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.GGHawkLink
{
	public partial class GGHawkLink : ISaveRam
	{
		public byte[] CloneSaveRam()
		{
			if ((L.SaveRAM != null) || (R.SaveRAM != null))
			{
				int Len1 = 0;
				int Len2 = 0;
				int index = 0;

				if (L.SaveRAM != null)
				{
					Len1 = L.SaveRAM.Length;
				}

				if (R.SaveRAM != null)
				{
					Len2 = R.SaveRAM.Length;
				}

				byte[] temp = new byte[Len1 + Len2];

				if (L.SaveRAM != null)
				{
					for (int i = 0; i < L.SaveRAM.Length; i++)
					{
						temp[index] = L.SaveRAM[i];
						index++;
					}
				}

				if (R.SaveRAM != null)
				{
					for (int i = 0; i < L.SaveRAM.Length; i++)
					{
						temp[index] = R.SaveRAM[i];
						index++;
					}
				}

				return temp;
			}
			else
			{
				return null;
			}
		}

		public void StoreSaveRam(byte[] data)
		{
			if ((L.SaveRAM != null) && (R.SaveRAM == null))
			{
				Buffer.BlockCopy(data, 0, L.SaveRAM, 0, L.SaveRAM.Length);
			}
			else if ((R.SaveRAM != null) && (L.SaveRAM == null))
			{
				Buffer.BlockCopy(data, 0, R.SaveRAM, 0, R.SaveRAM.Length);
			}
			else if ((R.SaveRAM != null) && (L.SaveRAM != null))
			{
				Buffer.BlockCopy(data, 0, L.SaveRAM, 0, L.SaveRAM.Length);
				Buffer.BlockCopy(data, L.SaveRAM.Length, R.SaveRAM, 0, R.SaveRAM.Length);
			}

			Console.WriteLine("loading SRAM here");
		}

		public bool SaveRamModified
		{
			get 
			{
				return linkSyncSettings.Use_SRAM;
			}	
		}
	}
}
