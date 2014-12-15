using System;
using BizHawk.Emulation.Common;
using EMU7800.Core;

namespace BizHawk.Emulation.Cores.Atari.Atari7800
{
	public partial class Atari7800 : ISaveRam
	{
		public byte[] CloneSaveRam()
		{
			return (byte[])hsram.Clone();
		}
		public void StoreSaveRam(byte[] data)
		{
			Buffer.BlockCopy(data, 0, hsram, 0, data.Length);
		}

		public bool SaveRamModified
		{
			get
			{
				return GameInfo.MachineType == MachineType.A7800PAL || GameInfo.MachineType == MachineType.A7800NTSC;
			}
		}
	}
}
