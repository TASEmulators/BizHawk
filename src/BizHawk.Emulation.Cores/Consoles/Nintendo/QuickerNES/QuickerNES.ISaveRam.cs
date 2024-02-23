using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.QuickerNES
{
	public partial class QuickerNES : ISaveRam
	{
		public byte[] CloneSaveRam()
		{
			LibQuickerNES.ThrowStringError(QN.qn_battery_ram_save(Context, _saveRamBuff, _saveRamBuff.Length));
			return (byte[])_saveRamBuff.Clone();
		}

		public void StoreSaveRam(byte[] data)
		{
			LibQuickerNES.ThrowStringError(QN.qn_battery_ram_load(Context, data, data.Length));
		}

		public bool SaveRamModified => QN.qn_has_battery_ram(Context);

		private byte[] _saveRamBuff;

		private void InitSaveRamBuff()
		{
			int size = 0;
			LibQuickerNES.ThrowStringError(QN.qn_battery_ram_size(Context, ref size));
			_saveRamBuff = new byte[size];
		}
	}
}
