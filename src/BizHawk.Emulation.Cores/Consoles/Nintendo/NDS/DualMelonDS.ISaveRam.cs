using System;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	partial class DualNDS : ISaveRam
	{
		public bool SaveRamModified => L.SaveRamModified || R.SaveRamModified;

		public byte[] CloneSaveRam()
		{
			var lb = L.CloneSaveRam()!;
			var rb = R.CloneSaveRam()!;
			byte[] ret = new byte[lb.Length + rb.Length];
			Buffer.BlockCopy(lb, 0, ret, 0, lb.Length);
			Buffer.BlockCopy(rb, 0, ret, lb.Length, rb.Length);
			return ret;
		}

		public void StoreSaveRam(byte[] data)
		{
			var lb = new byte[L.CloneSaveRam()!.Length];
			var rb = new byte[R.CloneSaveRam()!.Length];
			Buffer.BlockCopy(data, 0, lb, 0, lb.Length);
			Buffer.BlockCopy(data, lb.Length, rb, 0, rb.Length);
			L.StoreSaveRam(lb);
			R.StoreSaveRam(rb);
		}
	}
}
