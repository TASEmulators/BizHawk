using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.PCEngine
{
	public sealed partial class PCEngine : ISaveRam
	{
		public bool SaveRamModified { get; private set; }

		public byte[] CloneSaveRam()
		{
			if (BRAM != null)
			{
				return (byte[])BRAM.Clone();
			}
			else
			{
				return null;
			}
		}

		public void StoreSaveRam(byte[] data)
		{
			if (BRAM != null)
			{
				Array.Copy(data, BRAM, data.Length);
			}
		}
	}
}
