using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.WonderSwan
{
	partial class WonderSwan : ISaveRam
	{
		private byte[] _saveramBuff;

		private void InitISaveRam()
		{
			_saveramBuff = new byte[BizSwan.bizswan_saveramsize(Core)];
		}

		public byte[] CloneSaveRam()
		{
			if (!BizSwan.bizswan_saveramsave(Core, _saveramBuff, _saveramBuff.Length))
			{
				throw new InvalidOperationException($"{nameof(BizSwan.bizswan_saveramsave)}() returned false!");
			}

			return (byte[])_saveramBuff.Clone();
		}

		public void StoreSaveRam(byte[] data)
		{
			if (!BizSwan.bizswan_saveramload(Core, data, data.Length))
			{
				throw new InvalidOperationException($"{nameof(BizSwan.bizswan_saveramload)}() returned false!");
			}
		}

		public bool SaveRamModified => BizSwan.bizswan_saveramsize(Core) > 0;
	}
}
