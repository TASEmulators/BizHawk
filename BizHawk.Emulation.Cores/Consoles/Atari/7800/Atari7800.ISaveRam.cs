using System;
using BizHawk.Emulation.Common;
using EMU7800.Core;

namespace BizHawk.Emulation.Cores.Atari.Atari7800
{
	public partial class Atari7800 : ISaveRam
	{
		public byte[] CloneSaveRam()
		{
			return (byte[])_hsram.Clone();
		}

		public void StoreSaveRam(byte[] data)
		{
			Buffer.BlockCopy(data, 0, _hsram, 0, data.Length);
		}

		public bool SaveRamModified => _gameInfo.MachineType == MachineType.A7800PAL
			|| _gameInfo.MachineType == MachineType.A7800NTSC;
	}
}
