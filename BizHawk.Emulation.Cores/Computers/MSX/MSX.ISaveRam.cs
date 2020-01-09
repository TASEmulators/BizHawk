using System;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.MSX
{
	public partial class MSX : ISaveRam
	{
		public byte[] CloneSaveRam()
		{
			if (SaveRAM != null)
			{
				return (byte[])SaveRAM.Clone();
			}
			else
			{
				return null;
			}
		}

		public void StoreSaveRam(byte[] data)
		{
			if (SaveRAM != null)
			{
				Array.Copy(data, SaveRAM, data.Length);
			}
		}

		public bool SaveRamModified { get; private set; }

		public byte[] SaveRAM;
		private byte SaveRamBank;
	}
}
