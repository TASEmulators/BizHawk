using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	public partial class A7800Hawk : ISaveRam
	{
		public byte[] CloneSaveRam(bool clearDirty)
		{
			return (byte[])_hsram.Clone();
		}

		public void StoreSaveRam(byte[] data)
		{
			if (data.Length != _hsram.Length) throw new InvalidOperationException("Incorrect sram size.");
			Buffer.BlockCopy(data, 0, _hsram, 0, data.Length);
		}

		public bool SaveRamModified => (_hsbios != null);
	}
}
