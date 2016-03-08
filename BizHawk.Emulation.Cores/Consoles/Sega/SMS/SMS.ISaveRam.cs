using System;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public sealed partial class SMS : ISaveRam
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

		private byte[] SaveRAM;
		private byte SaveRamBank;
	}
}
