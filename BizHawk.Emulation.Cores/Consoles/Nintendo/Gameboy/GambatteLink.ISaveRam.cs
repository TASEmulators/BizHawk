using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class GambatteLink : ISaveRam
	{
		public bool SaveRamModified
		{
			get
			{
				return L.SaveRamModified || R.SaveRamModified;
			}
		}

		public byte[] CloneSaveRam()
		{
			byte[] lb = L.CloneSaveRam();
			byte[] rb = R.CloneSaveRam();
			byte[] ret = new byte[lb.Length + rb.Length];
			Buffer.BlockCopy(lb, 0, ret, 0, lb.Length);
			Buffer.BlockCopy(rb, 0, ret, lb.Length, rb.Length);
			return ret;
		}

		public void StoreSaveRam(byte[] data)
		{
			byte[] lb = new byte[L.CloneSaveRam().Length];
			byte[] rb = new byte[R.CloneSaveRam().Length];
			Buffer.BlockCopy(data, 0, lb, 0, lb.Length);
			Buffer.BlockCopy(data, lb.Length, rb, 0, rb.Length);
			L.StoreSaveRam(lb);
			R.StoreSaveRam(rb);
		}
	}
}
