using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	public partial class GBHawk : ISaveRam
	{
		public byte[] CloneSaveRam()
		{
			return (byte[])_sram.Clone();
		}

		public void StoreSaveRam(byte[] data)
		{
			Buffer.BlockCopy(data, 0, _sram, 0, data.Length);
		}

		public bool SaveRamModified
		{
			get 
			{
				return false;
			}	
		}
	}
}
