using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	public partial class A7800Hawk : ISaveRam
	{
		public byte[] CloneSaveRam()
		{
			return (byte[])_hsram.Clone();
		}

		public void StoreSaveRam(byte[] data)
		{
			Buffer.BlockCopy(data, 0, _hsram, 0, data.Length);
		}

		public bool SaveRamModified => (_hsbios != null);
	}
}
